using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterFoldersAppServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public EfCoreFileCenterFoldersAppServiceTests()
    {
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Should_Create_And_List_Folders_With_Paging()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Beta" });
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Alpha" });
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Gamma" });

            var result = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SkipCount = 0,
                    MaxResultCount = 2
                });

            result.TotalCount.ShouldBe(3);
            result.Items.Select(item => item.Name).ShouldBe(new[] { "Alpha", "Beta" });
            result.Items.ShouldAllBe(item => item.OwnerId == userId);
        });
    }

    [Fact]
    public async Task Should_Rename_Move_And_Delete_Folder()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var source = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Source" });
            var target = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Target" });
            var child = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput
                {
                    ParentId = source.Id,
                    Name = "Child"
                });

            var renamed = await _foldersAppService.RenameAsync(
                child.Id,
                new PrivateCloudDrive.FileCenter.RenameFileNodeInput { Name = "Renamed" });

            renamed.Name.ShouldBe("Renamed");

            var moved = await _foldersAppService.MoveAsync(
                renamed.Id,
                new PrivateCloudDrive.FileCenter.MoveFileNodeInput { ParentId = target.Id });

            moved.ParentId.ShouldBe(target.Id);

            var targetList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    ParentId = target.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            targetList.TotalCount.ShouldBe(1);
            targetList.Items.Single().Name.ShouldBe("Renamed");

            await _foldersAppService.DeleteAsync(moved.Id);

            var afterDelete = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    ParentId = target.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            afterDelete.TotalCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Folder_Name()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Documents" });

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "documents" });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists);
        });
    }

    [Fact]
    public async Task Should_Reject_Move_To_Self_Child_Folder()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var root = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Root" });
            var child = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput
                {
                    ParentId = root.Id,
                    Name = "Child"
                });
            var grandchild = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput
                {
                    ParentId = child.Id,
                    Name = "Grandchild"
                });

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.MoveAsync(
                    root.Id,
                    new PrivateCloudDrive.FileCenter.MoveFileNodeInput { ParentId = grandchild.Id });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterCannotMoveToSelfOrDescendant);
        });
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "file-center-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
