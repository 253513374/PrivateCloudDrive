using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterSharesAndTagsTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterSharesAppService _sharesAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterPublicSharesAppService _publicSharesAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterTagsAppService _tagsAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public EfCoreFileCenterSharesAndTagsTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _sharesAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterSharesAppService>();
        _publicSharesAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterPublicSharesAppService>();
        _tagsAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterTagsAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Should_Create_And_Access_Password_Protected_File_Share()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("shared text");

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("shared.txt", content);

            var share = await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id,
                    Password = "secret",
                    AllowDownload = true,
                    ExpirationTime = DateTime.Now.AddDays(1)
                });

            share.Token.Length.ShouldBeGreaterThan(20);
            share.RequiresPassword.ShouldBeTrue();

            var publicSummary = await _publicSharesAppService.GetAsync(share.Token);
            publicSummary.PasswordRequired.ShouldBeTrue();
            publicSummary.FileName.ShouldBe("shared.txt");

            var wrongPassword = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _publicSharesAppService.VerifyPasswordAsync(
                    share.Token,
                    new PrivateCloudDrive.FileCenter.VerifySharePasswordInput { Password = "wrong" });
            });

            wrongPassword.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterSharePasswordInvalid);

            var verified = await _publicSharesAppService.VerifyPasswordAsync(
                share.Token,
                new PrivateCloudDrive.FileCenter.VerifySharePasswordInput { Password = "secret" });

            verified.PasswordRequired.ShouldBeFalse();

            var download = await _publicSharesAppService.GetDownloadAsync(share.Token, "secret");
            await using (download.Content)
            {
                using var downloaded = new MemoryStream();
                await download.Content.CopyToAsync(downloaded);
                downloaded.ToArray().ShouldBe(content);
            }
        });
    }

    [Fact]
    public async Task Should_Reject_Expired_Or_Disabled_Shares()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("expired text");

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("expired.txt", content);
            var expiredShare = await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id,
                    ExpirationTime = DateTime.Now.AddMinutes(-1)
                });

            var expired = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _publicSharesAppService.GetAsync(expiredShare.Token);
            });

            expired.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterShareExpired);

            var activeShare = await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id
                });

            await _sharesAppService.DeleteAsync(activeShare.Id);

            var disabled = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _publicSharesAppService.GetAsync(activeShare.Token);
            });

            disabled.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterShareNotFound);
        });
    }

    [Fact]
    public async Task Should_Tag_And_Favorite_Files_Then_Filter_List()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var image = await UploadTextFileAsync("family.jpg", Encoding.UTF8.GetBytes("image"));
            var document = await UploadTextFileAsync("notes.txt", Encoding.UTF8.GetBytes("document"));
            var tag = await _tagsAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileTagInput
                {
                    Name = "Family",
                    Color = "#1F7A5C"
                });

            var duplicate = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _tagsAppService.CreateAsync(
                    new PrivateCloudDrive.FileCenter.CreateFileTagInput { Name = "family" });
            });

            duplicate.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterTagAlreadyExists);

            await _tagsAppService.AddToNodeAsync(image.Id, tag.Id);
            var favorite = await _tagsAppService.SetFavoriteAsync(
                image.Id,
                new PrivateCloudDrive.FileCenter.SetFileFavoriteInput { IsFavorite = true });

            favorite.IsFavorite.ShouldBeTrue();

            var taggedList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    TagId = tag.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            taggedList.TotalCount.ShouldBe(1);
            taggedList.Items.Single().Id.ShouldBe(image.Id);

            var favoriteList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    IsFavorite = true,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            favoriteList.TotalCount.ShouldBe(1);
            favoriteList.Items.Single().Id.ShouldBe(image.Id);

            await _tagsAppService.RemoveFromNodeAsync(image.Id, tag.Id);

            var afterRemove = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    TagId = tag.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            afterRemove.TotalCount.ShouldBe(0);
            document.Id.ShouldNotBe(image.Id);
        });
    }

    private async Task<PrivateCloudDrive.FileCenter.FileNodeDto> UploadTextFileAsync(
        string fileName,
        byte[] content)
    {
        await using var stream = new MemoryStream(content);
        return await _fileUploadService.UploadSmallFileAsync(
            parentId: null,
            fileName,
            "text/plain",
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
                    new Claim(AbpClaimTypes.UserName, "shares-tags-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
