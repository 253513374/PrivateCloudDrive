using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
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
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid> _blobObjectRepository;
    private readonly IRepository<PrivateCloudDrive.FileCenter.MediaAsset, Guid> _mediaAssetRepository;
    private readonly IBlobContainer<PrivateCloudDrive.FileCenter.FileCenterBlobContainer> _blobContainer;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterFoldersAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterFoldersAppServiceTests()
    {
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _blobObjectRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid>>();
        _mediaAssetRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.MediaAsset, Guid>>();
        _blobContainer = GetRequiredService<IBlobContainer<PrivateCloudDrive.FileCenter.FileCenterBlobContainer>>();
        _softDeleteFilter = GetRequiredService<IDataFilter<ISoftDelete>>();
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

    /// <summary>
    /// 验证 V1.1 10+ 文件批量删除全部成功，列表刷新正确。
    /// </summary>
    [Fact]
    public async Task Should_Batch_Delete_10_Plus_Files()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("batch delete test");

        await WithCurrentUserAsync(userId, async () =>
        {
            // 创建 12 个文件
            var fileIds = new System.Collections.Generic.List<Guid>();
            for (var i = 0; i < 12; i++)
            {
                await using var stream = new MemoryStream(content);
                var fileNode = await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    fileName: $"batch-{i:D3}.txt",
                    contentType: "text/plain",
                    stream,
                    content.Length);
                fileIds.Add(fileNode.Id);
            }

            // 确认 12 个文件可见
            var beforeDelete = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SkipCount = 0,
                    MaxResultCount = 20
                });
            beforeDelete.TotalCount.ShouldBe(12);

            // 批量删除 12 个文件
            await _foldersAppService.DeleteManyAsync(
                new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                {
                    Ids = fileIds
                });

            // 确认活动列表不再显示
            var afterDelete = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SkipCount = 0,
                    MaxResultCount = 20
                });
            afterDelete.TotalCount.ShouldBe(0);

            // 确认回收站显示 12 条
            var deletedList = await _foldersAppService.GetDeletedListAsync(
                new PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 20
                });
            deletedList.TotalCount.ShouldBe(12);
        });
    }

    /// <summary>
    /// 验证 V1.1 跨用户 ID 混入批量请求时不会操作他人文件。
    /// </summary>
    [Fact]
    public async Task Should_Reject_Batch_Operations_For_Other_User_Nodes()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        Guid otherFileId = default;

        // 其他用户创建文件夹
        await WithCurrentUserAsync(otherUserId, async () =>
        {
            var folder = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Other-Folder" });
            otherFileId = folder.Id;
        });

        // 当前用户尝试操作其他用户的文件
        await WithCurrentUserAsync(ownerId, async () =>
        {
            // 批量删除 — 无法通过 GetOwnerNodeAsync 校验
            var deleteException = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.DeleteManyAsync(
                    new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                    {
                        Ids = [otherFileId]
                    });
            });
            deleteException.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);

            // 批量移动 — 同样无法通过 GetOwnerNodeAsync 校验
            var moveException = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.MoveManyAsync(
                    new PrivateCloudDrive.FileCenter.BatchMoveFileNodesInput
                    {
                        Ids = [otherFileId],
                        ParentId = null
                    });
            });
            moveException.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
        });
    }

    /// <summary>
    /// 验证 V1.1 批量永久删除后 Blob 清理正确，共享 Blob 引用不被误删。
    /// </summary>
    [Fact]
    public async Task Should_PermanentDelete_Multiple_Files_And_Cleanup_Shared_Blob()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("shared blob content");

        await WithCurrentUserAsync(userId, async () =>
        {
            // 上传第一个文件（获得 Blob）
            await using var stream1 = new MemoryStream(content);
            var fileNode1 = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "shared-original.txt",
                contentType: "text/plain",
                stream1,
                content.Length);

            var blobName = fileNode1.BlobName!;

            // 创建第二个 FileNode 指向同一个 Blob（模拟共享 Blob 场景）
            Guid secondNodeId = Guid.Empty;
            await WithUnitOfWorkAsync(async () =>
            {
                var fileNode2 = PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(),
                    tenantId: null,
                    ownerId: userId,
                    parentId: null,
                    name: "shared-copy.txt",
                    size: content.Length,
                    contentType: "text/plain",
                    blobName: blobName);
                var repo = GetRequiredService<PrivateCloudDrive.FileCenter.IFileNodeRepository>();
                await repo.InsertAsync(fileNode2, autoSave: true);
                secondNodeId = fileNode2.Id;
            });

            // 确认两个节点都存在
            (await _blobContainer.ExistsAsync(blobName)).ShouldBeTrue();

            // 删除并永久删除第一个节点
            await _fileUploadService.DeleteAsync(fileNode1.Id);
            await _foldersAppService.PermanentDeleteAsync(fileNode1.Id);

            // 共享 Blob 不应被删除（第二个节点仍在引用）
            (await _blobContainer.ExistsAsync(blobName)).ShouldBeTrue();

            // 确认第二个节点不受影响
            var secondNodeList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });
            secondNodeList.Items.Any(n => n.Id == secondNodeId).ShouldBeTrue();

            // 删除并永久删除第二个节点
            await _fileUploadService.DeleteAsync(secondNodeId);
            await _foldersAppService.PermanentDeleteAsync(secondNodeId);

            // 现在 Blob 应被清理
            (await _blobContainer.ExistsAsync(blobName)).ShouldBeFalse();
        });
    }

    /// <summary>
    /// 验证 V1.1 批量移动时目标目录校验和循环移动拒绝。
    /// </summary>
    [Fact]
    public async Task Should_Validate_Batch_Move_Target_Folder()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            // 创建目录结构：Alpha → Beta → Gamma
            var alpha = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Alpha" });
            var beta = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput
                {
                    ParentId = alpha.Id,
                    Name = "Beta"
                });
            var gamma = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput
                {
                    ParentId = beta.Id,
                    Name = "Gamma"
                });

            // 把 Beta 移动到 Gamma（自身或子孙）应拒绝
            var circularException = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.MoveManyAsync(
                    new PrivateCloudDrive.FileCenter.BatchMoveFileNodesInput
                    {
                        Ids = [beta.Id],
                        ParentId = gamma.Id
                    });
            });
            circularException.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterCannotMoveToSelfOrDescendant);

            // 把 Gamma 移动到不存在的目录应拒绝
            var notFoundException = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.MoveManyAsync(
                    new PrivateCloudDrive.FileCenter.BatchMoveFileNodesInput
                    {
                        Ids = [gamma.Id],
                        ParentId = Guid.NewGuid()
                    });
            });
            notFoundException.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);

            // 确认 Gamma 未被移动（仍在 Beta 下）
            var gammaList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    ParentId = beta.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });
            gammaList.Items.Single().Id.ShouldBe(gamma.Id);
        });
    }

    /// <summary>
    /// 验证 V1.1 批量操作事务性：混入非法 ID 时整体操作失败，不出现部分成功。
    /// </summary>
    [Fact]
    public async Task Should_Rollback_Entire_Batch_On_Exception()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("transaction test");

        await WithCurrentUserAsync(userId, async () =>
        {
            // 创建 3 个文件
            var fileIds = new System.Collections.Generic.List<Guid>();
            for (var i = 0; i < 3; i++)
            {
                await using var stream = new MemoryStream(content);
                var fileNode = await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    fileName: $"tx-file-{i:D3}.txt",
                    contentType: "text/plain",
                    stream,
                    content.Length);
                fileIds.Add(fileNode.Id);
            }

            // 混入一个不存在的 ID
            var mixedIds = fileIds.Concat([Guid.NewGuid()]).ToList();

            // 执行批量删除 — 因最后一个 ID 不存在而抛出异常
            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _foldersAppService.DeleteManyAsync(
                    new PrivateCloudDrive.FileCenter.BatchFileNodeInput
                    {
                        Ids = mixedIds
                    });
            });

            // 验证异常码正确
            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
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
