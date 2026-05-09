using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
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
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterMediaLibraryAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterMediaLibraryAppServiceTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _mediaLibraryAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterMediaLibraryAppService>();
        _tagsAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterTagsAppService>();
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
