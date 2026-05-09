using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心EfCoreFileCenterFoldersAppServiceTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterFoldersAppServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterFoldersAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterFoldersAppServiceTests()
    {
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Restore_And_Permanently_Delete_Folder_Tree()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var root = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Archive" });
            var child = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput
                {
                    ParentId = root.Id,
                    Name = "Child"
                });

            await _foldersAppService.DeleteAsync(root.Id);

            var deletedList = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            deletedList.TotalCount.ShouldBe(1);
            deletedList.Items.Single().Id.ShouldBe(root.Id);
            deletedList.Items.ShouldNotContain(item => item.Id == child.Id);

            var restored = await _foldersAppService.RestoreAsync(root.Id);
            restored.Id.ShouldBe(root.Id);

            var rootList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            rootList.Items.Single(item => item.Id == root.Id).Name.ShouldBe("Archive");

            var childList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    ParentId = root.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            childList.TotalCount.ShouldBe(1);
            childList.Items.Single().Id.ShouldBe(child.Id);

            await _foldersAppService.DeleteAsync(root.Id);
            await _foldersAppService.PermanentDeleteAsync(root.Id);

            var afterPermanentDelete = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            afterPermanentDelete.TotalCount.ShouldBe(0);

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.RestoreAsync(root.Id);
            });
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Empty_Trash_For_Current_User()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var first = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "First" });
            var second = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Second" });

            await _foldersAppService.DeleteAsync(first.Id);
            await _foldersAppService.DeleteAsync(second.Id);

            var beforeEmpty = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            beforeEmpty.TotalCount.ShouldBe(2);

            await _foldersAppService.EmptyTrashAsync();

            var afterEmpty = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            afterEmpty.TotalCount.ShouldBe(0);

            await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.RestoreAsync(first.Id);
            });
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reject_Restore_When_Active_Name_Already_Exists()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var deleted = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Archive" });

            await _foldersAppService.DeleteAsync(deleted.Id);
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Archive" });

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.RestoreAsync(deleted.Id);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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

    /// <summary>
    /// 验证 V1.1 当前目录搜索只返回名称匹配的直属节点。
    /// </summary>
    [Fact]
    public async Task Should_Search_Folders_By_Keyword_In_Current_Folder()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Holiday Photos" });
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Work Docs" });

            var result = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SearchKeyword = "photo",
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            result.TotalCount.ShouldBe(1);
            result.Items.Single().Name.ShouldBe("Holiday Photos");
        });
    }

    /// <summary>
    /// 验证 V1.1 全盘搜索不会返回其他用户的节点。
    /// </summary>
    [Fact]
    public async Task Should_Not_Return_Other_User_Nodes_When_Searching_All()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await WithCurrentUserAsync(ownerId, async () =>
        {
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Owner Search Target" });
        });

        await WithCurrentUserAsync(otherUserId, async () =>
        {
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Other Search Target" });
        });

        await WithCurrentUserAsync(ownerId, async () =>
        {
            var result = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SearchKeyword = "Search Target",
                    SearchScope = PrivateCloudDrive.FileCenter.FileCenterSearchScope.All,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            result.TotalCount.ShouldBe(1);
            result.Items.Single().Name.ShouldBe("Owner Search Target");
            result.Items.ShouldAllBe(item => item.OwnerId == ownerId);
        });
    }

    /// <summary>
    /// 验证 V1.1 排序参数可以按名称倒序返回列表。
    /// </summary>
    [Fact]
    public async Task Should_Sort_Folders_By_Name_Descending()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Alpha" });
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Beta" });
            await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Gamma" });

            var result = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    Sorting = "name desc",
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            result.Items.Select(item => item.Name).ShouldBe(new[] { "Gamma", "Beta", "Alpha" });
        });
    }

    /// <summary>
    /// 验证 V1.1 批量移动、收藏、回收站恢复和永久删除共用同一套节点规则。
    /// </summary>
    [Fact]
    public async Task Should_Batch_Move_Favorite_Delete_Restore_And_Permanent_Delete_Folders()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var target = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Target" });
            var first = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "First" });
            var second = await _foldersAppService.CreateAsync(new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Second" });

            var moved = await _foldersAppService.MoveManyAsync(
                new PrivateCloudDrive.FileCenter.BatchMoveFileNodesInput
                {
                    Ids = [first.Id, second.Id],
                    ParentId = target.Id
                });

            moved.Select(item => item.ParentId).ShouldAllBe(parentId => parentId == target.Id);

            var favorites = await _foldersAppService.SetFavoriteManyAsync(
                new PrivateCloudDrive.FileCenter.BatchSetFavoriteInput
                {
                    Ids = [first.Id, second.Id],
                    IsFavorite = true
                });

            favorites.ShouldAllBe(item => item.IsFavorite);

            await _foldersAppService.DeleteManyAsync(
                new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                {
                    Ids = [first.Id, second.Id]
                });

            var deletedList = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            deletedList.Items.Select(item => item.Id).ShouldBe([first.Id, second.Id], ignoreOrder: true);

            var restored = await _foldersAppService.RestoreManyAsync(
                new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                {
                    Ids = [first.Id, second.Id]
                });

            restored.Select(item => item.Id).ShouldBe([first.Id, second.Id], ignoreOrder: true);

            await _foldersAppService.DeleteManyAsync(
                new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                {
                    Ids = [first.Id, second.Id]
                });
            await _foldersAppService.PermanentDeleteManyAsync(
                new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                {
                    Ids = [first.Id, second.Id]
                });

            var afterPermanentDelete = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            afterPermanentDelete.Items.Select(item => item.Id).ShouldNotContain(first.Id);
            afterPermanentDelete.Items.Select(item => item.Id).ShouldNotContain(second.Id);
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
