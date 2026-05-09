using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shouldly;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心EfCoreFileCenterMediaAssetTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterMediaAssetTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterChunkUploadService _chunkUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService _fileDownloadService;
    private readonly PrivateCloudDrive.FileCenter.FileCenterMediaAssetProcessingJob _mediaAssetProcessingJob;
    private readonly IRepository<PrivateCloudDrive.FileCenter.MediaAsset, Guid> _mediaAssetRepository;
    private readonly IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid> _blobObjectRepository;
    private readonly IRepository<BackgroundJobRecord, Guid> _backgroundJobRepository;
    private readonly IBlobContainer<PrivateCloudDrive.FileCenter.FileCenterBlobContainer> _blobContainer;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterMediaAssetTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterMediaAssetTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _chunkUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterChunkUploadService>();
        _fileDownloadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService>();
        _mediaAssetProcessingJob = GetRequiredService<PrivateCloudDrive.FileCenter.FileCenterMediaAssetProcessingJob>();
        _mediaAssetRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.MediaAsset, Guid>>();
        _blobObjectRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid>>();
        _backgroundJobRepository = GetRequiredService<IRepository<BackgroundJobRecord, Guid>>();
        _blobContainer = GetRequiredService<IBlobContainer<PrivateCloudDrive.FileCenter.FileCenterBlobContainer>>();
        _softDeleteFilter = GetRequiredService<IDataFilter<ISoftDelete>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Process_Image_And_Expose_Thumbnail()
    {
        var userId = Guid.NewGuid();
        var content = await CreatePngAsync();

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "tiny.png",
                contentType: "image/png",
                stream,
                content.Length);

            var pendingAsset = await GetMediaAssetAsync(fileNode.Id);
            pendingAsset.ShouldNotBeNull();
            pendingAsset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Pending);

            await _mediaAssetProcessingJob.ExecuteAsync(
                new PrivateCloudDrive.FileCenter.MediaAssetProcessingJobArgs
                {
                    MediaAssetId = pendingAsset.Id,
                    FileNodeId = fileNode.Id
                });

            var completedAsset = await GetMediaAssetAsync(fileNode.Id);
            completedAsset.ShouldNotBeNull();
            completedAsset.ProcessError.ShouldBeNull();
            completedAsset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Completed);
            completedAsset.Width.ShouldBe(1);
            completedAsset.Height.ShouldBe(1);
            completedAsset.ThumbnailBlobObjectId.ShouldNotBeNull();

            var thumbnail = await _fileDownloadService.GetThumbnailAsync(fileNode.Id);
            thumbnail.ContentType.ShouldBe("image/jpeg");
            thumbnail.FileName.ShouldBe("tiny.thumbnail.jpg");

            await using (thumbnail.Content)
            {
                using var thumbnailBytes = new MemoryStream();
                await thumbnail.Content.CopyToAsync(thumbnailBytes);

                thumbnailBytes.Length.ShouldBeGreaterThan(0);
            }
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Remove_MediaAsset_And_Thumbnail_When_File_Is_Permanently_Deleted()
    {
        var userId = Guid.NewGuid();
        var content = await CreatePngAsync();

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "purged-image.png",
                contentType: "image/png",
                stream,
                content.Length);

            var pendingAsset = await GetMediaAssetAsync(fileNode.Id);
            pendingAsset.ShouldNotBeNull();

            await _mediaAssetProcessingJob.ExecuteAsync(
                new PrivateCloudDrive.FileCenter.MediaAssetProcessingJobArgs
                {
                    MediaAssetId = pendingAsset.Id,
                    FileNodeId = fileNode.Id
                });

            var completedAsset = await GetMediaAssetAsync(fileNode.Id);
            completedAsset.ShouldNotBeNull();
            completedAsset.ThumbnailBlobObjectId.ShouldNotBeNull();

            var originalBlobName = fileNode.BlobName!;
            var thumbnailBlobName = await GetBlobNameAsync(completedAsset.ThumbnailBlobObjectId.Value);

            (await _blobContainer.ExistsAsync(originalBlobName)).ShouldBeTrue();
            (await _blobContainer.ExistsAsync(thumbnailBlobName)).ShouldBeTrue();

            await _fileUploadService.DeleteAsync(fileNode.Id);
            await _foldersAppService.PermanentDeleteAsync(fileNode.Id);

            (await _blobContainer.ExistsAsync(originalBlobName)).ShouldBeFalse();
            (await _blobContainer.ExistsAsync(thumbnailBlobName)).ShouldBeFalse();
            (await GetMediaAssetCountIncludingDeletedAsync(fileNode.Id)).ShouldBe(0);
            (await GetBlobObjectCountIncludingDeletedAsync(originalBlobName)).ShouldBe(0);
            (await GetBlobObjectCountIncludingDeletedAsync(thumbnailBlobName)).ShouldBe(0);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Process_Video_And_Expose_Cover_Thumbnail()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("fake mp4 bytes");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "clip.mp4",
                contentType: "video/mp4",
                stream,
                content.Length);

            var pendingAsset = await GetMediaAssetAsync(fileNode.Id);
            pendingAsset.ShouldNotBeNull();
            pendingAsset.MediaType.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetMediaType.Video);
            pendingAsset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Pending);

            await _mediaAssetProcessingJob.ExecuteAsync(
                new PrivateCloudDrive.FileCenter.MediaAssetProcessingJobArgs
                {
                    MediaAssetId = pendingAsset.Id,
                    FileNodeId = fileNode.Id
                });

            var completedAsset = await GetMediaAssetAsync(fileNode.Id);
            completedAsset.ShouldNotBeNull();
            completedAsset.ProcessError.ShouldBeNull();
            completedAsset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Completed);
            completedAsset.Width.ShouldBe(640);
            completedAsset.Height.ShouldBe(360);
            completedAsset.DurationMilliseconds.ShouldBe(123456);
            completedAsset.Codec.ShouldBe("h264");
            completedAsset.ThumbnailBlobObjectId.ShouldNotBeNull();
            completedAsset.MetadataJson.ShouldNotBeNull();
            completedAsset.MetadataJson.ShouldContain("DurationMilliseconds");

            var thumbnail = await _fileDownloadService.GetThumbnailAsync(fileNode.Id);
            thumbnail.ContentType.ShouldBe("image/jpeg");
            thumbnail.FileName.ShouldBe("clip.thumbnail.jpg");

            await using (thumbnail.Content)
            {
                using var thumbnailBytes = new MemoryStream();
                await thumbnail.Content.CopyToAsync(thumbnailBytes);

                thumbnailBytes.ToArray().ShouldBe(TestFileCenterVideoProcessor.ThumbnailBytes);
            }
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Create_Pending_MediaAsset_And_BackgroundJob_For_Image_Upload()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("fake image bytes");

        await WithCurrentUserAsync(userId, async () =>
        {
            var jobCountBeforeUpload = await GetBackgroundJobCountAsync();

            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "photo.jpg",
                contentType: "image/jpeg",
                stream,
                content.Length);

            var mediaAsset = await GetMediaAssetAsync(fileNode.Id);

            mediaAsset.ShouldNotBeNull();
            mediaAsset.OwnerId.ShouldBe(userId);
            mediaAsset.FileNodeId.ShouldBe(fileNode.Id);
            mediaAsset.MediaType.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetMediaType.Image);
            mediaAsset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Pending);

            var jobCountAfterUpload = await GetBackgroundJobCountAsync();
            jobCountAfterUpload.ShouldBe(jobCountBeforeUpload + 1);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Create_Pending_MediaAsset_And_BackgroundJob_For_Chunked_Video_Upload()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("fake video bytes through chunks");
        var chunkSize = 5;
        var chunks = Split(content, chunkSize);

        await WithCurrentUserAsync(userId, async () =>
        {
            var jobCountBeforeUpload = await GetBackgroundJobCountAsync();

            var session = await _chunkUploadService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateUploadSessionInput
                {
                    FileName = "clip.mp4",
                    ContentType = "video/mp4",
                    TotalSize = content.Length,
                    ChunkSize = chunkSize,
                    TotalChunks = chunks.Count
                });

            for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                await UploadChunkAsync(session.Id, chunkIndex, chunks[chunkIndex]);
            }

            var fileNode = await _chunkUploadService.CompleteAsync(session.Id);
            var mediaAsset = await GetMediaAssetAsync(fileNode.Id);

            mediaAsset.ShouldNotBeNull();
            mediaAsset.OwnerId.ShouldBe(userId);
            mediaAsset.FileNodeId.ShouldBe(fileNode.Id);
            mediaAsset.MediaType.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetMediaType.Video);
            mediaAsset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Pending);

            var jobCountAfterUpload = await GetBackgroundJobCountAsync();
            jobCountAfterUpload.ShouldBe(jobCountBeforeUpload + 1);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Not_Create_MediaAsset_For_NonMedia_File()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("plain document");

        await WithCurrentUserAsync(userId, async () =>
        {
            var jobCountBeforeUpload = await GetBackgroundJobCountAsync();

            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "notes.txt",
                contentType: "text/plain",
                stream,
                content.Length);

            var mediaAsset = await GetMediaAssetAsync(fileNode.Id);
            mediaAsset.ShouldBeNull();

            var jobCountAfterUpload = await GetBackgroundJobCountAsync();
            jobCountAfterUpload.ShouldBe(jobCountBeforeUpload);
        });
    }

    private async Task UploadChunkAsync(Guid sessionId, int chunkIndex, byte[] content)
    {
        await using var stream = new MemoryStream(content);

        await _chunkUploadService.UploadChunkAsync(
            sessionId,
            chunkIndex,
            stream,
            content.Length);
    }

    private Task<PrivateCloudDrive.FileCenter.MediaAsset?> GetMediaAssetAsync(Guid fileNodeId)
    {
        return WithUnitOfWorkAsync(async () =>
            await _mediaAssetRepository.FirstOrDefaultAsync(asset => asset.FileNodeId == fileNodeId));
    }

    private Task<long> GetBackgroundJobCountAsync()
    {
        return WithUnitOfWorkAsync(async () =>
            await _backgroundJobRepository.GetCountAsync());
    }

    private Task<string> GetBlobNameAsync(Guid blobObjectId)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var blobObject = await _blobObjectRepository.GetAsync(blobObjectId);

            return blobObject.BlobName;
        });
    }

    private Task<int> GetMediaAssetCountIncludingDeletedAsync(Guid fileNodeId)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            using (_softDeleteFilter.Disable())
            {
                var mediaAssets = await _mediaAssetRepository.GetListAsync(asset => asset.FileNodeId == fileNodeId);

                return mediaAssets.Count;
            }
        });
    }

    private Task<int> GetBlobObjectCountIncludingDeletedAsync(string blobName)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            using (_softDeleteFilter.Disable())
            {
                var blobObjects = await _blobObjectRepository.GetListAsync(blob => blob.BlobName == blobName);

                return blobObjects.Count;
            }
        });
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "media-asset-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }

    private static List<byte[]> Split(byte[] content, int chunkSize)
    {
        var chunks = new List<byte[]>();
        for (var offset = 0; offset < content.Length; offset += chunkSize)
        {
            chunks.Add(content.Skip(offset).Take(chunkSize).ToArray());
        }

        return chunks;
    }

    private static async Task<byte[]> CreatePngAsync()
    {
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = new Rgba32(255, 0, 0, 255);

        await using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);

        return stream.ToArray();
    }
}
