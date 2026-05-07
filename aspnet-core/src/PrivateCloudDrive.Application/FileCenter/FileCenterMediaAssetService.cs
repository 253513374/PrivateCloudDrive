using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterMediaAssetService
{
    Task<MediaAsset?> CreatePendingAssetAsync(FileNode fileNode);
}

public class FileCenterMediaAssetService : IFileCenterMediaAssetService, ITransientDependency
{
    private static readonly string[] ImageExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".heif"
    };

    private static readonly string[] VideoExtensions =
    {
        ".mp4", ".m4v", ".mov", ".mkv", ".webm", ".avi"
    };

    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IGuidGenerator _guidGenerator;

    public FileCenterMediaAssetService(
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        IBackgroundJobManager backgroundJobManager,
        IGuidGenerator guidGenerator)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _backgroundJobManager = backgroundJobManager;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public virtual async Task<MediaAsset?> CreatePendingAssetAsync(FileNode fileNode)
    {
        if (fileNode.NodeType != FileNodeType.File)
        {
            return null;
        }

        var mediaType = DetectMediaType(fileNode.Name, fileNode.ContentType);
        if (mediaType == null)
        {
            return null;
        }

        var existingAsset = await _mediaAssetRepository.FirstOrDefaultAsync(
            asset => asset.FileNodeId == fileNode.Id);

        if (existingAsset != null)
        {
            return existingAsset;
        }

        var mediaAsset = MediaAsset.CreatePending(
            _guidGenerator.Create(),
            fileNode.TenantId,
            fileNode.OwnerId,
            fileNode.Id,
            mediaType.Value);

        await _mediaAssetRepository.InsertAsync(mediaAsset, autoSave: true);

        await _backgroundJobManager.EnqueueAsync(
            new MediaAssetProcessingJobArgs
            {
                MediaAssetId = mediaAsset.Id,
                FileNodeId = fileNode.Id
            });

        return mediaAsset;
    }

    private static MediaAssetMediaType? DetectMediaType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return MediaAssetMediaType.Image;
            }

            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return MediaAssetMediaType.Video;
            }
        }

        var extension = Path.GetExtension(fileName);
        if (ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return MediaAssetMediaType.Image;
        }

        if (VideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return MediaAssetMediaType.Video;
        }

        return null;
    }
}
