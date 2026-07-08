using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 执行FileCenterMediaAssetProcessingJob后台任务，处理异步业务流程并避免阻塞用户请求。
/// </summary>
public class FileCenterMediaAssetProcessingJob
    : AsyncBackgroundJob<MediaAssetProcessingJobArgs>, ITransientDependency
{
    private const int ThumbnailMaxLength = 320;

    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly IBlobContainer<FileCenterBlobContainer> _blobContainer;
    private readonly IFileCenterBlobStorageService _blobStorageService;
    private readonly IFileCenterVideoProcessor _videoProcessor;

    /// <summary>
    /// 初始化 <see cref="FileCenterMediaAssetProcessingJob"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterMediaAssetProcessingJob(
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        IFileNodeRepository fileNodeRepository,
        IBlobContainer<FileCenterBlobContainer> blobContainer,
        IFileCenterBlobStorageService blobStorageService,
        IFileCenterVideoProcessor videoProcessor)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _fileNodeRepository = fileNodeRepository;
        _blobContainer = blobContainer;
        _blobStorageService = blobStorageService;
        _videoProcessor = videoProcessor;
    }

    /// <summary>
    /// 处理异步或耗时业务任务，并产出后续流程所需的结果。
    /// </summary>
    [UnitOfWork]
    public override async Task ExecuteAsync(MediaAssetProcessingJobArgs args)
    {
        var mediaAsset = await _mediaAssetRepository.GetAsync(args.MediaAssetId);

        mediaAsset.MarkProcessing();
        await _mediaAssetRepository.UpdateAsync(mediaAsset, autoSave: true);

        try
        {
            var fileNode = await _fileNodeRepository.GetAsync(args.FileNodeId);

            if (string.IsNullOrWhiteSpace(fileNode.BlobName))
            {
                throw new InvalidOperationException("Media file node has no blob name.");
            }

            if (mediaAsset.MediaType == MediaAssetMediaType.Image)
            {
                await ProcessImageAsync(mediaAsset, fileNode);
            }
            else if (mediaAsset.MediaType == MediaAssetMediaType.Video)
            {
                await ProcessVideoAsync(mediaAsset, fileNode);
            }
        }
        catch (Exception exception)
        {
            mediaAsset.MarkFailed(FileCenterMediaLibraryHelpers.SanitizeProcessError(exception.Message) ?? exception.Message);
            await _mediaAssetRepository.UpdateAsync(mediaAsset, autoSave: true);
        }
    }

    private async Task ProcessImageAsync(MediaAsset mediaAsset, FileNode fileNode)
    {
        await using var imageStream = await _blobContainer.GetAsync(fileNode.BlobName!);
        using var image = await Image.LoadAsync(imageStream);

        image.Mutate(context => context.AutoOrient());

        var takenAt = GetTakenAt(image);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(ThumbnailMaxLength, ThumbnailMaxLength)
        }));

        await using var thumbnailStream = new MemoryStream();
        await image.SaveAsync(thumbnailStream, new JpegEncoder { Quality = 82 });
        thumbnailStream.Position = 0;

        var thumbnail = await _blobStorageService.SaveAsync(
            fileNode.OwnerId,
            $"{Path.GetFileNameWithoutExtension(fileNode.Name)}.thumbnail.jpg",
            "image/jpeg",
            thumbnailStream,
            thumbnailStream.Length);

        var metadataJson = JsonSerializer.Serialize(
            new
            {
                OriginalWidth = originalWidth,
                OriginalHeight = originalHeight,
                ThumbnailWidth = image.Width,
                ThumbnailHeight = image.Height
            });

        mediaAsset.MarkImageProcessed(
            originalWidth,
            originalHeight,
            takenAt,
            thumbnail.Id,
            metadataJson);

        await _mediaAssetRepository.UpdateAsync(mediaAsset, autoSave: true);
    }

    private async Task ProcessVideoAsync(MediaAsset mediaAsset, FileNode fileNode)
    {
        await using var videoStream = await _blobContainer.GetAsync(fileNode.BlobName!);
        var result = await _videoProcessor.ProcessAsync(videoStream, fileNode.Name);

        await using var thumbnailStream = new MemoryStream(result.ThumbnailBytes);
        var thumbnail = await _blobStorageService.SaveAsync(
            fileNode.OwnerId,
            $"{Path.GetFileNameWithoutExtension(fileNode.Name)}.thumbnail.jpg",
            "image/jpeg",
            thumbnailStream,
            thumbnailStream.Length);

        mediaAsset.MarkVideoProcessed(
            result.Width,
            result.Height,
            result.DurationMilliseconds,
            result.Codec,
            thumbnail.Id,
            result.MetadataJson);

        await _mediaAssetRepository.UpdateAsync(mediaAsset, autoSave: true);
    }

    private static DateTime? GetTakenAt(Image image)
    {
        if (image.Metadata.ExifProfile?.TryGetValue(
                ExifTag.DateTimeOriginal,
                out IExifValue<string>? exifValue) != true)
        {
            return null;
        }

        if (exifValue is null || exifValue.Value is not { } value)
        {
            return null;
        }

        return DateTime.TryParseExact(
            value,
            "yyyy:MM:dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var takenAt)
            ? takenAt
            : null;
    }
}
