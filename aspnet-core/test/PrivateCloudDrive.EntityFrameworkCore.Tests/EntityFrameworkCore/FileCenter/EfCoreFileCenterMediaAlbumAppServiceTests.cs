using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterMediaAlbumAppServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterMediaAlbumsAppService _mediaAlbumsAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterMediaLibraryAppService _mediaLibraryAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly ICurrentTenant _currentTenant;

    public EfCoreFileCenterMediaAlbumAppServiceTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _mediaAlbumsAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterMediaAlbumsAppService>();
        _mediaLibraryAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterMediaLibraryAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Should_Create_Album_And_Add_Media_Items()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var album = await _mediaAlbumsAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput
                {
                    Name = "旅行",
                    Description = "五月照片和视频"
                });
            var photo = await UploadSmallFileAsync("trip-photo.jpg", "image/jpeg");
            var video = await UploadSmallFileAsync("trip-video.mp4", "video/mp4");

            var duplicate = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _mediaAlbumsAppService.CreateAsync(
                    new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = " 旅行 " });
            });

            duplicate.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumAlreadyExists);

            var added = await _mediaAlbumsAppService.AddItemsAsync(
                album.Id,
                new PrivateCloudDrive.FileCenter.AddMediaAlbumItemsInput
                {
                    FileNodeIds = [photo.Id, video.Id, photo.Id]
                });

            added.Count.ShouldBe(2);

            var albumItems = await _mediaAlbumsAppService.GetItemsAsync(
                album.Id,
                new PagedResultRequestDto { MaxResultCount = 20 });
            albumItems.TotalCount.ShouldBe(2);
            albumItems.Items.Select(item => item.Id).ShouldContain(photo.Id);
            albumItems.Items.Select(item => item.Id).ShouldContain(video.Id);

            var savedAlbum = await _mediaAlbumsAppService.GetAsync(album.Id);
            savedAlbum.ItemsCount.ShouldBe(2);
            savedAlbum.CoverFileNodeId.ShouldBe(photo.Id);

            var changedCover = await _mediaAlbumsAppService.SetCoverAsync(
                album.Id,
                new PrivateCloudDrive.FileCenter.SetMediaAlbumCoverInput { FileNodeId = video.Id });
            changedCover.CoverFileNodeId.ShouldBe(video.Id);
        });
    }

    [Fact]
    public async Task Should_Reject_NonMedia_File_When_Adding_To_Album()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var album = await _mediaAlbumsAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "资料" });
            var document = await UploadSmallFileAsync("notes.txt", "text/plain");

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _mediaAlbumsAppService.AddItemsAsync(
                    album.Id,
                    new PrivateCloudDrive.FileCenter.AddMediaAlbumItemsInput
                    {
                        FileNodeIds = [document.Id]
                    });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyMediaFileCanBeManaged);
        });
    }

    [Fact]
    public async Task Should_Reject_Other_User_File_When_Adding_To_Album()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        Guid firstUserPhotoId = Guid.Empty;

        await WithCurrentUserAsync(firstUserId, async () =>
        {
            var photo = await UploadSmallFileAsync("private-photo.jpg", "image/jpeg");
            firstUserPhotoId = photo.Id;
        });

        await WithCurrentUserAsync(secondUserId, async () =>
        {
            var album = await _mediaAlbumsAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "我的相册" });

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _mediaAlbumsAppService.AddItemsAsync(
                    album.Id,
                    new PrivateCloudDrive.FileCenter.AddMediaAlbumItemsInput
                    {
                        FileNodeIds = [firstUserPhotoId]
                    });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
        });
    }

    [Fact]
    public async Task Should_Remove_Album_Item_Without_Deleting_File()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var album = await _mediaAlbumsAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "待整理" });
            var photo = await UploadSmallFileAsync("keep-photo.jpg", "image/jpeg");

            await _mediaAlbumsAppService.AddItemsAsync(
                album.Id,
                new PrivateCloudDrive.FileCenter.AddMediaAlbumItemsInput
                {
                    FileNodeIds = [photo.Id]
                });

            await _mediaAlbumsAppService.RemoveItemAsync(album.Id, photo.Id);

            var albumItems = await _mediaAlbumsAppService.GetItemsAsync(
                album.Id,
                new PagedResultRequestDto { MaxResultCount = 20 });
            albumItems.TotalCount.ShouldBe(0);

            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });
            timeline.Items.Select(item => item.Id).ShouldContain(photo.Id);
        });
    }

    [Fact]
    public async Task Should_Delete_Album_Without_Deleting_Files()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var album = await _mediaAlbumsAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "临时相册" });
            var photo = await UploadSmallFileAsync("album-file-survives.jpg", "image/jpeg");

            await _mediaAlbumsAppService.AddItemsAsync(
                album.Id,
                new PrivateCloudDrive.FileCenter.AddMediaAlbumItemsInput
                {
                    FileNodeIds = [photo.Id]
                });

            await _mediaAlbumsAppService.DeleteAsync(album.Id);

            var missingAlbum = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _mediaAlbumsAppService.GetAsync(album.Id);
            });
            missingAlbum.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumNotFound);

            var timeline = await _mediaLibraryAppService.GetTimelineAsync(
                new PrivateCloudDrive.FileCenter.GetMediaTimelineInput
                {
                    MaxResultCount = 20
                });
            timeline.Items.Select(item => item.Id).ShouldContain(photo.Id);
        });
    }

    /// <summary>
    /// 跨租户隔离：租户B用户不应看到租户A用户的相册列表。
    /// </summary>
    [Fact]
    public async Task Should_Enforce_Tenant_Isolation_For_Album_List()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();

        await WithCurrentUserAsync(userIdA, async () =>
        {
            using (_currentTenant.Change(tenantAId))
            {
                await _mediaAlbumsAppService.CreateAsync(
                    new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "租户A相册" });
            }
        });

        await WithCurrentUserAsync(userIdB, async () =>
        {
            using (_currentTenant.Change(tenantBId))
            {
                var albums = await _mediaAlbumsAppService.GetListAsync(
                    new PagedResultRequestDto { MaxResultCount = 20 });

                albums.Items.ShouldBeEmpty();
                albums.TotalCount.ShouldBe(0);
            }
        });
    }

    /// <summary>
    /// 跨租户隔离：租户B用户无法获取租户A用户的相册详情（应返回 NotFound）。
    /// </summary>
    [Fact]
    public async Task Should_Enforce_Tenant_Isolation_For_Album_Get()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();
        PrivateCloudDrive.FileCenter.MediaAlbumDto? tenantAAlbum = null;

        await WithCurrentUserAsync(userIdA, async () =>
        {
            using (_currentTenant.Change(tenantAId))
            {
                tenantAAlbum = await _mediaAlbumsAppService.CreateAsync(
                    new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "租户A私密相册" });
            }
        });

        await WithCurrentUserAsync(userIdB, async () =>
        {
            using (_currentTenant.Change(tenantBId))
            {
                var exception = await Should.ThrowAsync<BusinessException>(async () =>
                    await _mediaAlbumsAppService.GetAsync(tenantAAlbum!.Id));

                exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumNotFound);
            }
        });
    }

    /// <summary>
    /// 跨租户隔离：租户B用户无法将租户A用户的文件添加到自己的相册中（应返回 NotFound）。
    /// </summary>
    [Fact]
    public async Task Should_Enforce_Tenant_Isolation_For_Album_AddItems()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();
        PrivateCloudDrive.FileCenter.FileNodeDto? tenantAPhoto = null;
        PrivateCloudDrive.FileCenter.MediaAlbumDto? tenantBAlbum = null;

        await WithCurrentUserAsync(userIdA, async () =>
        {
            using (_currentTenant.Change(tenantAId))
            {
                tenantAPhoto = await UploadSmallFileAsync("tenant-a-photo.jpg", "image/jpeg");
            }
        });

        await WithCurrentUserAsync(userIdB, async () =>
        {
            using (_currentTenant.Change(tenantBId))
            {
                tenantBAlbum = await _mediaAlbumsAppService.CreateAsync(
                    new PrivateCloudDrive.FileCenter.CreateMediaAlbumInput { Name = "租户B相册" });
            }
        });

        // 在 tenantB 上下文中尝试添加 tenantA 的文件
        await WithCurrentUserAsync(userIdB, async () =>
        {
            using (_currentTenant.Change(tenantBId))
            {
                var exception = await Should.ThrowAsync<BusinessException>(async () =>
                    await _mediaAlbumsAppService.AddItemsAsync(
                        tenantBAlbum!.Id,
                        new PrivateCloudDrive.FileCenter.AddMediaAlbumItemsInput
                        {
                            FileNodeIds = [tenantAPhoto!.Id]
                        }));

                exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
            }
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
                    new Claim(AbpClaimTypes.UserName, "media-album-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
