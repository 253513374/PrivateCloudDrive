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
    public async Task Should_Paginate_Timeline_Correctly()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            // Upload 5 media items
            var items = new[]
            {
                await UploadSmallFileAsync("img-a.png", "image/png"),
                await UploadSmallFileAsync("img-b.png", "image/png"),
                await UploadSmallFileAsync("img-c.png", "image/png"),
                await UploadSmallFileAsync("img-d.png", "image/png"),
                await UploadSmallFileAsync("img-e.png", "image/png")
            };

            // Mark all processed so they have assets
            foreach (var item in items)
            {
                await MarkImageProcessedAsync(item.Id, DateTime.Now);
            }

            // First page: 2 items
            var page1 = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    SkipCount = 0,
                    MaxResultCount = 2
                });

            page1.TotalCount.ShouldBe(5);
            page1.Items.Count.ShouldBe(2);

            // Second page: 2 items
            var page2 = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    SkipCount = 2,
                    MaxResultCount = 2
                });

            page2.TotalCount.ShouldBe(5);
            page2.Items.Count.ShouldBe(2);

            // Items should not overlap between pages
            var page1Ids = page1.Items.Select(i => i.Id).ToHashSet();
            var page2Ids = page2.Items.Select(i => i.Id).ToHashSet();
            page1Ids.Intersect(page2Ids).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task Should_Paginate_Timeline_At_Boundary()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            // Upload 3 media items
            for (var i = 0; i < 3; i++)
            {
                var item = await UploadSmallFileAsync($"boundary-{i}.jpg", "image/jpeg");
                await MarkImageProcessedAsync(item.Id, DateTime.Now.AddDays(-i));
            }

            // Request exactly at the total boundary
            var page = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    SkipCount = 2,
                    MaxResultCount = 3
                });

            page.TotalCount.ShouldBe(3);
            page.Items.Count.ShouldBe(1);

            // Request beyond total boundary
            var emptyPage = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    SkipCount = 10,
                    MaxResultCount = 10
                });

            emptyPage.TotalCount.ShouldBe(3);
            emptyPage.Items.Count.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Filter_Timeline_By_ProcessStatus_At_Db()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var pending = await UploadSmallFileAsync("pending-status.jpg", "image/jpeg");
            var failed = await UploadSmallFileAsync("failed-status.mp4", "video/mp4");
            var completed = await UploadSmallFileAsync("completed-status.jpg", "image/jpeg");

            await MarkFailedAsync(failed.Id, "failed deliberately");
            await MarkImageProcessedAsync(completed.Id, DateTime.Now);
            // Note: pending item has no asset yet - semantically Pending

            // Filter by Pending
            var pendingResult = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    ProcessStatus = PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Pending,
                    MaxResultCount = 20
                });

            pendingResult.Items.Select(i => i.Name).ShouldContain("pending-status.jpg");
            pendingResult.Items.Select(i => i.Name).ShouldNotContain("failed-status.mp4");
            pendingResult.Items.Select(i => i.Name).ShouldNotContain("completed-status.jpg");

            // Filter by Failed
            var failedResult = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    ProcessStatus = PrivateCloudDrive.FileCenter.MediaAssetProcessStatus.Failed,
                    MaxResultCount = 20
                });

            failedResult.Items.Select(i => i.Name).ShouldContain("failed-status.mp4");
            failedResult.Items.Select(i => i.Name).ShouldNotContain("pending-status.jpg");
            failedResult.Items.Select(i => i.Name).ShouldNotContain("completed-status.jpg");
        });
    }

    [Fact]
    public async Task Should_Filter_Timeline_By_TimeRange_At_Db()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var oldItem = await UploadSmallFileAsync("old-item.jpg", "image/jpeg");
            var midItem = await UploadSmallFileAsync("mid-item.jpg", "image/jpeg");
            var newItem = await UploadSmallFileAsync("new-item.jpg", "image/jpeg");

            var now = DateTime.UtcNow;
            await MarkImageProcessedAsync(oldItem.Id, now.AddDays(-10));
            await MarkImageProcessedAsync(midItem.Id, now.AddDays(-5));
            await MarkImageProcessedAsync(newItem.Id, now.AddDays(1));

            // Filter by StartTime
            var fromMidResult = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    StartTime = now.AddDays(-7),
                    MaxResultCount = 20
                });

            fromMidResult.Items.Select(i => i.Name).ShouldNotContain("old-item.jpg");
            fromMidResult.Items.Select(i => i.Name).ShouldContain("mid-item.jpg");
            fromMidResult.Items.Select(i => i.Name).ShouldContain("new-item.jpg");

            // Filter by time range
            var rangeResult = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    StartTime = now.AddDays(-7),
                    EndTime = now.AddDays(-2),
                    MaxResultCount = 20
                });

            rangeResult.Items.Select(i => i.Name).ShouldNotContain("old-item.jpg");
            rangeResult.Items.Select(i => i.Name).ShouldContain("mid-item.jpg");
            rangeResult.Items.Select(i => i.Name).ShouldNotContain("new-item.jpg");
        });
    }

    [Fact]
    public async Task Should_Not_Include_NonMedia_Files_In_Timeline()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await UploadSmallFileAsync("photo.jpg", "image/jpeg");
            await UploadSmallFileAsync("notes.txt", "text/plain");
            await UploadSmallFileAsync("data.csv", "text/csv");
            await UploadSmallFileAsync("script.js", "application/javascript");

            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });

            timeline.Items.Select(i => i.Name).ShouldContain("photo.jpg");
            timeline.Items.Select(i => i.Name).ShouldNotContain("notes.txt");
            timeline.Items.Select(i => i.Name).ShouldNotContain("data.csv");
            timeline.Items.Select(i => i.Name).ShouldNotContain("script.js");
        });
    }

    [Fact]
    public async Task Should_Handle_Empty_Timeline()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });

            timeline.TotalCount.ShouldBe(0);
            timeline.Items.ShouldBeEmpty();
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
            asset.MarkProcessing();
            asset.MarkImageProcessed(1920, 1080, takenAt, Guid.NewGuid());
            await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);
        });
    }

    private async Task MarkVideoProcessedAsync(Guid fileNodeId, long durationMilliseconds = 123456)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var asset = await GetMediaAssetAsync(fileNodeId);
            asset.MarkProcessing();
            asset.MarkVideoProcessed(1280, 720, durationMilliseconds, "h264", Guid.NewGuid());
            await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);
        });
    }

    private async Task MarkFailedAsync(Guid fileNodeId, string error)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var asset = await GetMediaAssetAsync(fileNodeId);
            asset.MarkProcessing();
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
