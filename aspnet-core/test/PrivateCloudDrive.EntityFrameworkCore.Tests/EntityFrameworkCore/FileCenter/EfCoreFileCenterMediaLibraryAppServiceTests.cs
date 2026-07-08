using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心EfCoreFileCenterMediaLibraryAppServiceTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterMediaLibraryAppServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterMediaLibraryAppService _mediaLibraryAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterTagsAppService _tagsAppService;
    private readonly IRepository<PrivateCloudDrive.FileCenter.MediaAsset, Guid> _mediaAssetRepository;
    private readonly IRepository<PrivateCloudDrive.FileCenter.FileCenterOperationLog, Guid> _fileCenterOperationLogRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterMediaLibraryAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterMediaLibraryAppServiceTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _mediaLibraryAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterMediaLibraryAppService>();
        _tagsAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterTagsAppService>();
        _mediaAssetRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.MediaAsset, Guid>>();
        _fileCenterOperationLogRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.FileCenterOperationLog, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_List_Images_And_Videos_Separately()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await UploadSmallFileAsync("photo.jpg", "image/jpeg");
            await UploadSmallFileAsync("clip.mp4", "video/mp4");
            await UploadSmallFileAsync("notes.txt", "text/plain");

            var images = await _mediaLibraryAppService.GetImagesAsync(new PrivateCloudDrive.FileCenter.GetMediaFilesInput
            {
                MaxResultCount = 20
            });
            var videos = await _mediaLibraryAppService.GetVideosAsync(new PrivateCloudDrive.FileCenter.GetMediaFilesInput
            {
                MaxResultCount = 20
            });

            images.Items.Select(item => item.Name).ShouldContain("photo.jpg");
            images.Items.Select(item => item.Name).ShouldNotContain("clip.mp4");
            images.Items.Select(item => item.Name).ShouldNotContain("notes.txt");

            videos.Items.Select(item => item.Name).ShouldContain("clip.mp4");
            videos.Items.Select(item => item.Name).ShouldNotContain("photo.jpg");
            videos.Items.Select(item => item.Name).ShouldNotContain("notes.txt");
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Filter_Media_Library_By_Favorite()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var favoriteImage = await UploadSmallFileAsync("favorite-photo.png", "image/png");
            await UploadSmallFileAsync("plain-photo.png", "image/png");

            await _tagsAppService.SetFavoriteAsync(
                favoriteImage.Id,
                new PrivateCloudDrive.FileCenter.SetFileFavoriteInput
                {
                    IsFavorite = true
                });

            var images = await _mediaLibraryAppService.GetImagesAsync(new PrivateCloudDrive.FileCenter.GetMediaFilesInput
            {
                IsFavorite = true,
                MaxResultCount = 20
            });

            images.Items.Select(item => item.Name).ShouldContain("favorite-photo.png");
            images.Items.Select(item => item.Name).ShouldNotContain("plain-photo.png");
        });
    }

    [Fact]
    public async Task Should_Get_Mixed_Media_Timeline_Ordered_By_TimelineTime_Descending()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var oldPhoto = await UploadSmallFileAsync("old-photo.jpg", "image/jpeg");
            var clip = await UploadSmallFileAsync("middle-clip.mp4", "video/mp4");
            var latestPhoto = await UploadSmallFileAsync("latest-photo.jpg", "image/jpeg");

            await MarkImageProcessedAsync(oldPhoto.Id, DateTime.Now.AddDays(-10));
            await MarkVideoProcessedAsync(clip.Id);
            await MarkImageProcessedAsync(latestPhoto.Id, DateTime.Now.AddDays(1));

            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });

            timeline.TotalCount.ShouldBe(3);
            timeline.Items[0].Name.ShouldBe("latest-photo.jpg");
            timeline.Items.Last().Name.ShouldBe("old-photo.jpg");
            timeline.Items.Select(item => item.Name).ShouldContain("middle-clip.mp4");
        });
    }

    [Fact]
    public async Task Should_Use_TakenAt_Before_CreationTime_For_Timeline()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var recentlyUploaded = await UploadSmallFileAsync("recent-upload.jpg", "image/jpeg");
            var olderUploadWithFutureTakenAt = await UploadSmallFileAsync("camera-taken-at.jpg", "image/jpeg");

            await MarkImageProcessedAsync(recentlyUploaded.Id, DateTime.Now.AddDays(-2));
            await MarkImageProcessedAsync(olderUploadWithFutureTakenAt.Id, DateTime.Now.AddDays(2));

            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });

            timeline.Items[0].Id.ShouldBe(olderUploadWithFutureTakenAt.Id);
        });
    }

    [Fact]
    public async Task Should_Filter_Timeline_By_MediaType()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await UploadSmallFileAsync("timeline-photo.png", "image/png");
            await UploadSmallFileAsync("timeline-video.mp4", "video/mp4");

            var images = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MediaType = PrivateCloudDrive.FileCenter.MediaAssetMediaType.Image,
                    MaxResultCount = 20
                });
            var videos = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MediaType = PrivateCloudDrive.FileCenter.MediaAssetMediaType.Video,
                    MaxResultCount = 20
                });

            images.Items.Select(item => item.Name).ShouldContain("timeline-photo.png");
            images.Items.Select(item => item.Name).ShouldNotContain("timeline-video.mp4");
            videos.Items.Select(item => item.Name).ShouldContain("timeline-video.mp4");
            videos.Items.Select(item => item.Name).ShouldNotContain("timeline-photo.png");
        });
    }

    [Fact]
    public async Task Should_Not_Return_Other_User_Media_In_Timeline()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        await WithCurrentUserAsync(firstUserId, async () =>
        {
            await UploadSmallFileAsync("first-user-photo.jpg", "image/jpeg");
        });

        await WithCurrentUserAsync(secondUserId, async () =>
        {
            await UploadSmallFileAsync("second-user-photo.jpg", "image/jpeg");

            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });

            timeline.Items.Select(item => item.Name).ShouldContain("second-user-photo.jpg");
            timeline.Items.Select(item => item.Name).ShouldNotContain("first-user-photo.jpg");
        });
    }

    [Fact]
    public async Task Should_Return_Media_Detail_With_ProcessStatus()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var clip = await UploadSmallFileAsync("detail-video.mp4", "video/mp4");
            await MarkVideoProcessedAsync(clip.Id, durationMilliseconds: 654321);

            var detail = await _mediaLibraryAppService.GetDetailAsync(clip.Id);

            detail.FileNodeId.ShouldBe(clip.Id);
            detail.MediaType.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetMediaType.Video);
            detail.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Completed);
            detail.DurationMilliseconds.ShouldBe(654321);
            detail.CanPreview.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Return_Processing_Status_Items()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var pending = await UploadSmallFileAsync("pending-photo.jpg", "image/jpeg");
            var failed = await UploadSmallFileAsync("failed-video.mp4", "video/mp4");
            var completed = await UploadSmallFileAsync("completed-photo.jpg", "image/jpeg");

            await MarkFailedAsync(failed.Id, "ffmpeg exited with code 1");
            await MarkImageProcessedAsync(completed.Id, DateTime.Now);

            var processing = await _mediaLibraryAppService.GetProcessingStatusAsync(
                new PrivateCloudDrive.FileCenter.GetMediaProcessingStatusInput
                {
                    MaxResultCount = 20
                });

            processing.Items.Select(item => item.Id).ShouldContain(pending.Id);
            processing.Items.Select(item => item.Id).ShouldContain(failed.Id);
            processing.Items.Select(item => item.Id).ShouldNotContain(completed.Id);

            var failedOnly = await _mediaLibraryAppService.GetProcessingStatusAsync(
                new PrivateCloudDrive.FileCenter.GetMediaProcessingStatusInput
                {
                    Status = PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Failed,
                    MaxResultCount = 20
                });

            failedOnly.TotalCount.ShouldBe(1);
            failedOnly.Items.Single().Id.ShouldBe(failed.Id);
        });
    }

    [Fact]
    public async Task Should_Not_Expose_Sensitive_ProcessError()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var clip = await UploadSmallFileAsync("secret-video.mp4", "video/mp4");
            await MarkFailedAsync(
                clip.Id,
                @"C:\Users\q4528\secrets\clip.mp4 token=abc123 password=pw");

            var detail = await _mediaLibraryAppService.GetDetailAsync(clip.Id);

            detail.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Failed);
            detail.ProcessErrorSummary.ShouldNotBeNull();
            detail.ProcessErrorSummary.ShouldNotContain(@"C:\");
            detail.ProcessErrorSummary.ShouldNotContain("abc123");
            detail.ProcessErrorSummary.ShouldNotContain("password=pw");
            detail.ProcessErrorSummary.ShouldContain("[redacted]");
        });
    }

    [Fact]
    public async Task Should_Retry_Own_Failed_Media_Successfully()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var clip = await UploadSmallFileAsync("retry-failed.mp4", "video/mp4");

            // First retry creates a pending asset and moves to Processing
            var firstRetry = await _mediaLibraryAppService.RetryProcessingAsync(clip.Id);
            firstRetry.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Processing);

            // Mark as Failed
            await MarkFailedAsync(clip.Id, "transient error");
            await WithUnitOfWorkAsync(async () =>
            {
                var asset = await GetMediaAssetAsync(clip.Id);
                asset.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Failed);
            });

            // Second retry should succeed from Failed → Processing
            var secondRetry = await _mediaLibraryAppService.RetryProcessingAsync(clip.Id);
            secondRetry.ProcessStatus.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Processing);
        });
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Retrying_Other_Users_Media()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        PrivateCloudDrive.FileCenter.FileNodeDto? firstUserClip = null;

        await WithCurrentUserAsync(firstUserId, async () =>
        {
            firstUserClip = await UploadSmallFileAsync("other-user-video.mp4", "video/mp4");
            await _mediaLibraryAppService.RetryProcessingAsync(firstUserClip.Id);
            await MarkFailedAsync(firstUserClip.Id, "some error");
        });

        await WithCurrentUserAsync(secondUserId, async () =>
        {
            // Second user cannot see/retry first user's file → throws BusinessException (NotFound)
            var exception = await Should.ThrowAsync<Volo.Abp.BusinessException>(async () =>
                await _mediaLibraryAppService.RetryProcessingAsync(firstUserClip!.Id));

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
        });
    }

    [Fact]
    public async Task Should_Reject_Retry_For_Completed_Media()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var clip = await UploadSmallFileAsync("completed-retry.mp4", "video/mp4");

            // First retry → Processing
            await _mediaLibraryAppService.RetryProcessingAsync(clip.Id);

            // Mark as Completed
            await MarkVideoProcessedAsync(clip.Id, durationMilliseconds: 9999);

            // Retry on Completed should throw
            var exception = await Should.ThrowAsync<Volo.Abp.BusinessException>(async () =>
                await _mediaLibraryAppService.RetryProcessingAsync(clip.Id));

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAssetCannotRetry);
        });
    }

    [Fact]
    public async Task Should_Reject_Retry_For_Processing_Media()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var clip = await UploadSmallFileAsync("processing-retry.mp4", "video/mp4");

            // First retry → Processing
            await _mediaLibraryAppService.RetryProcessingAsync(clip.Id);

            // Retry while still Processing should throw
            var exception = await Should.ThrowAsync<Volo.Abp.BusinessException>(async () =>
                await _mediaLibraryAppService.RetryProcessingAsync(clip.Id));

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAssetCannotRetry);
        });
    }

    [Fact]
    public async Task Should_Record_Audit_Log_On_Successful_Retry()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var clip = await UploadSmallFileAsync("audit-retry.mp4", "video/mp4");

            // First retry: Pending (from CreatePendingAssetAsync) → Processing
            await _mediaLibraryAppService.RetryProcessingAsync(clip.Id);

            // Verify audit log entry
            await WithUnitOfWorkAsync(async () =>
            {
                var logs = await _fileCenterOperationLogRepository.GetListAsync(
                    log => log.FileNodeId == clip.Id);

                logs.Count.ShouldBe(1);
                var log = logs[0];

                log.Action.ShouldBe(PrivateCloudDrive.FileCenter.FileCenterOperationLogConsts.ActionMediaRetry);
                log.StatusBefore.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Pending.ToString());
                log.StatusAfter.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Processing.ToString());
                log.MediaAssetId.ShouldNotBe(Guid.Empty);
                log.OperatorUserId.ShouldBe(userId);
            });

            // Mark Failed then retry again
            await MarkFailedAsync(clip.Id, "another error");
            await _mediaLibraryAppService.RetryProcessingAsync(clip.Id);

            // Verify second audit log: Failed → Processing
            await WithUnitOfWorkAsync(async () =>
            {
                var logs = await _fileCenterOperationLogRepository.GetListAsync(
                    log => log.FileNodeId == clip.Id);

                logs.Count.ShouldBe(2);
                var failedRetryLog = logs.OrderBy(l => l.CreationTime).Last();

                failedRetryLog.Action.ShouldBe(PrivateCloudDrive.FileCenter.FileCenterOperationLogConsts.ActionMediaRetry);
                failedRetryLog.StatusBefore.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Failed.ToString());
                failedRetryLog.StatusAfter.ShouldBe(PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Processing.ToString());
                failedRetryLog.OperatorUserId.ShouldBe(userId);
            });
        });
    }

    [Fact]
    public async Task Should_Not_Record_Audit_Log_On_Retry_Of_Other_User_Media()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        PrivateCloudDrive.FileCenter.FileNodeDto? firstUserClip = null;

        await WithCurrentUserAsync(firstUserId, async () =>
        {
            firstUserClip = await UploadSmallFileAsync("no-log-video.mp4", "video/mp4");
        });

        await WithCurrentUserAsync(secondUserId, async () =>
        {
            // Second user tries to retry — throws before any audit log is recorded
            await Should.ThrowAsync<Volo.Abp.BusinessException>(async () =>
                await _mediaLibraryAppService.RetryProcessingAsync(firstUserClip!.Id));

            // No audit log should exist for this FileNodeId
            await WithUnitOfWorkAsync(async () =>
            {
                var logs = await _fileCenterOperationLogRepository.GetListAsync(
                    log => log.FileNodeId == firstUserClip!.Id);

                logs.Count.ShouldBe(0);
            });
        });
    }

    private async Task<PrivateCloudDrive.FileCenter.FileNodeDto> UploadSmallFileAsync(
        string fileName,
        string contentType)
    {
        var content = Encoding.UTF8.GetBytes($"content for {fileName}");
        await using var stream = new MemoryStream(content);

        return await _fileUploadService.UploadSmallFileAsync(
            parentId: null,
            fileName,
            contentType,
            stream,
            content.Length);
    }

    private async Task MarkImageProcessedAsync(Guid fileNodeId, DateTime? takenAt)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var asset = await GetMediaAssetAsync(fileNodeId);
            asset.MarkImageProcessed(1920, 1080, takenAt, Guid.NewGuid());
            await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);
        });
    }

    private async Task MarkVideoProcessedAsync(Guid fileNodeId, long durationMilliseconds = 123456)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var asset = await GetMediaAssetAsync(fileNodeId);
            asset.MarkVideoProcessed(1280, 720, durationMilliseconds, "h264", Guid.NewGuid());
            await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);
        });
    }

    private async Task MarkFailedAsync(Guid fileNodeId, string error)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var asset = await GetMediaAssetAsync(fileNodeId);
            asset.MarkFailed(error);
            await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);
        });
    }

    private async Task<PrivateCloudDrive.FileCenter.MediaAsset> GetMediaAssetAsync(Guid fileNodeId)
    {
        var asset = await _mediaAssetRepository.FirstOrDefaultAsync(item => item.FileNodeId == fileNodeId);
        asset.ShouldNotBeNull();

        return asset;
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "media-library-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
