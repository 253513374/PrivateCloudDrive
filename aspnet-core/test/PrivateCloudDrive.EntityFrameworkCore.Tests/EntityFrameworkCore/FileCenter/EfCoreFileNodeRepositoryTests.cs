using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 提供EfCoreFileNodeRepositoryTests持久化访问能力，封装查询条件和数据存取细节。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileNodeRepositoryTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileNodeRepository _fileNodeRepository;
    private readonly IRepository<PrivateCloudDrive.FileCenter.FileNodeTag, Guid> _fileNodeTagRepository;
    private readonly IDataFilter<Volo.Abp.MultiTenancy.IMultiTenant> _multiTenantFilter;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileNodeRepositoryTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileNodeRepositoryTests()
    {
        _fileNodeRepository = GetRequiredService<PrivateCloudDrive.FileCenter.IFileNodeRepository>();
        _fileNodeTagRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.FileNodeTag, Guid>>();
        _multiTenantFilter = GetRequiredService<IDataFilter<Volo.Abp.MultiTenancy.IMultiTenant>>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Find_Node_By_Case_Insensitive_Name()
    {
        var ownerId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                    Guid.NewGuid(),
                    tenantId: null,
                    ownerId,
                    parentId: null,
                    "Photos"));
        });

        var node = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.FindByNameAsync(ownerId, parentId: null, "photos"));

        node.ShouldNotBeNull();
        node.Name.ShouldBe("Photos");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Get_Children_With_Folders_First_And_Count()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(),
                    tenantId: null,
                    ownerId,
                    parentId,
                    "zeta.txt",
                    size: 12,
                    contentType: "text/plain",
                    blobName: "files/zeta.txt"));

            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                    Guid.NewGuid(),
                    tenantId: null,
                    ownerId,
                    parentId,
                    "alpha"));
        });

        var children = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(ownerId, parentId, skipCount: 0, maxResultCount: 10));

        var count = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenCountAsync(ownerId, parentId));

        count.ShouldBe(2);
        children.Select(node => node.Name).ShouldBe(new[] { "alpha", "zeta.txt" });
        children[0].NodeType.ShouldBe(PrivateCloudDrive.FileCenter.FileNodeType.Folder);
        children[1].NodeType.ShouldBe(PrivateCloudDrive.FileCenter.FileNodeType.File);
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reject_Duplicate_Name_In_Same_Folder()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await _fileNodeRepository.InsertAsync(
                    PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                        Guid.NewGuid(),
                        tenantId: null,
                        ownerId,
                        parentId,
                        "Documents"));

                await _fileNodeRepository.InsertAsync(
                    PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                        Guid.NewGuid(),
                        tenantId: null,
                        ownerId,
                        parentId,
                        "documents"));
            });
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Allow_Recreate_Name_After_Soft_Delete()
    {
        var ownerId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                    Guid.NewGuid(),
                    tenantId: null,
                    ownerId,
                    parentId: null,
                    "Archive"));
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var existing = await _fileNodeRepository.FindByNameAsync(ownerId, parentId: null, "archive");
            existing.ShouldNotBeNull();

            await _fileNodeRepository.DeleteAsync(existing);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                    Guid.NewGuid(),
                    tenantId: null,
                    ownerId,
                    parentId: null,
                    "archive"));
        });

        var activeNode = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.FindByNameAsync(ownerId, parentId: null, "ARCHIVE"));

        var deletedNode = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.FindByNameAsync(ownerId, parentId: null, "Archive", includeDeleted: true));

        activeNode.ShouldNotBeNull();
        activeNode.IsDeleted.ShouldBeFalse();
        deletedNode.ShouldNotBeNull();
    }

    // ──────────────────────────────────────────────
    // V1.1 安全契约加固测试：搜索/筛选/排序安全
    // ──────────────────────────────────────────────

    /// <summary>
    /// 验证 V1.1 CurrentFolder 搜索只返回该父目录下的直属子节点，
    /// 不返回同级其他父目录的节点。
    /// </summary>
    [Fact]
    public async Task Should_Search_CurrentFolder_Only_Returns_That_Folders_Children()
    {
        var ownerId = Guid.NewGuid();
        var folderA = Guid.NewGuid();
        var folderB = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            // 在 folderA 下创建文件
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, folderA,
                    "report.txt", size: 100, contentType: "text/plain", blobName: "a/report.txt"));
            // 在 folderB 下创建同名文件
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, folderB,
                    "report.txt", size: 200, contentType: "text/plain", blobName: "b/report.txt"));
        });

        // 搜索 folderA → 只应返回 folderA 的节点
        var children = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, folderA, skipCount: 0, maxResultCount: 10,
                searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.CurrentFolder));

        children.Count.ShouldBe(1);
        children.Single().ParentId.ShouldBe(folderA);
    }

    /// <summary>
    /// 验证 V1.1 SearchScope=All 返回当前用户在所有文件夹中
    /// 匹配搜索关键字的未删除节点。
    /// </summary>
    [Fact]
    public async Task Should_Search_All_Returns_Users_All_Matching_Items()
    {
        var ownerId = Guid.NewGuid();
        var folderA = Guid.NewGuid();
        var folderB = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, folderA,
                    "Alpha Report.txt", size: 100, contentType: "text/plain", blobName: "a/alpha.txt"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, folderB,
                    "Beta Report.txt", size: 200, contentType: "text/plain", blobName: "b/beta.txt"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, folderA,
                    "Gamma Notes.txt", size: 50, contentType: "text/plain", blobName: "a/gamma.txt"));
        });

        // 全盘搜索 "Report" → 返回两个匹配
        var children = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId: null, skipCount: 0, maxResultCount: 10,
                searchKeyword: "Report",
                searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

        var count = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenCountAsync(
                ownerId, parentId: null,
                searchKeyword: "Report",
                searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

        count.ShouldBe(2);
        children.Select(n => n.Name).OrderBy(n => n).ShouldBe(new[] { "Alpha Report.txt", "Beta Report.txt" });
    }

    /// <summary>
    /// 验证 V1.1 跨用户隔离：用户 A 搜索不返回用户 B 的节点。
    /// 安全契约：所有查询必须限制 OwnerId。
    /// </summary>
    [Fact]
    public async Task Should_Not_Return_Other_User_Nodes()
    {
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerA, parentId: null,
                    "target.txt", size: 10, contentType: "text/plain", blobName: "a/target.txt"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerB, parentId: null,
                    "target.txt", size: 20, contentType: "text/plain", blobName: "b/target.txt"));
        });

        // 用户 A 搜索 → 只返回 A 的节点
        var children = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerA, parentId: null, skipCount: 0, maxResultCount: 10,
                searchKeyword: "target",
                searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

        var count = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenCountAsync(
                ownerA, parentId: null,
                searchKeyword: "target",
                searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

        count.ShouldBe(1);
        children.Single().OwnerId.ShouldBe(ownerA);
    }

    /// <summary>
    /// 验证 V1.1 跨租户隔离：租户 A 的搜索不返回租户 B 的节点。
    /// 安全契约：所有查询必须限制 TenantId。
    /// </summary>
    [Fact]
    public async Task Should_Not_Return_Other_Tenant_Nodes()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var ownerInTenantA = Guid.NewGuid();
        var ownerInTenantB = Guid.NewGuid();
        var nodeIdA = Guid.NewGuid();
        var nodeIdB = Guid.NewGuid();

        // 先验证插入成功（禁用 ABP 自动租户过滤以测试仓储自身的过滤逻辑）
        using (_multiTenantFilter.Disable())
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await _fileNodeRepository.InsertAsync(
                    PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                        nodeIdA, tenantA, ownerInTenantA, parentId: null,
                        "visible.txt", size: 10, contentType: "text/plain", blobName: "ta/visible.txt"));
                await _fileNodeRepository.InsertAsync(
                    PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                        nodeIdB, tenantB, ownerInTenantB, parentId: null,
                        "hidden.txt", size: 20, contentType: "text/plain", blobName: "tb/hidden.txt"));
            });
        }

        // 验证数据确实可查到（禁用自动租户过滤以避免 ABP MayHaveTenant 过滤掉 tenantId!=null 的数据）
        using (_multiTenantFilter.Disable())
        {
            var allNodes = await WithUnitOfWorkAsync(async () =>
                await _fileNodeRepository.GetListAsync());

            allNodes.Count.ShouldBeGreaterThanOrEqualTo(2);
            allNodes.ShouldContain(n => n.Id == nodeIdA);
            allNodes.ShouldContain(n => n.Id == nodeIdB);
        }

        // 以 A 身份列举 → 只应返回显式指定 tenantA 的节点（禁用自动租户过滤以测试仓储自身 TenantId 过滤）
        using (_multiTenantFilter.Disable())
        {
            var children = await WithUnitOfWorkAsync(async () =>
                await _fileNodeRepository.GetChildrenAsync(
                    ownerInTenantA, parentId: null, skipCount: 0, maxResultCount: 10,
                    tenantId: tenantA,
                    searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

            var count = await WithUnitOfWorkAsync(async () =>
                await _fileNodeRepository.GetChildrenCountAsync(
                    ownerInTenantA, parentId: null,
                    tenantId: tenantA,
                    searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

            count.ShouldBe(1);
            children.Single().Name.ShouldBe("visible.txt");
            children.Single().TenantId.ShouldBe(tenantA);
        }

        // 以 B 身份列举 → 只应看到 hidden.txt
        using (_multiTenantFilter.Disable())
        {
            var childrenB = await WithUnitOfWorkAsync(async () =>
                await _fileNodeRepository.GetChildrenAsync(
                    ownerInTenantB, parentId: null, skipCount: 0, maxResultCount: 10,
                    tenantId: tenantB,
                    searchScope: PrivateCloudDrive.FileCenter.FileCenterSearchScope.All));

            childrenB.Single().Name.ShouldBe("hidden.txt");
        }
    }

    /// <summary>
    /// 验证 V1.1 NodeType 筛选只返回指定类型的节点（文件夹/文件）。
    /// </summary>
    [Fact]
    public async Task Should_Filter_By_NodeType()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId, "FolderOnly"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId,
                    "file.txt", size: 10, contentType: "text/plain", blobName: "f/file.txt"));
        });

        // 筛选仅文件夹
        var folders = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                nodeType: PrivateCloudDrive.FileCenter.FileNodeType.Folder));

        folders.Count.ShouldBe(1);
        folders.Single().NodeType.ShouldBe(PrivateCloudDrive.FileCenter.FileNodeType.Folder);
        folders.Single().Name.ShouldBe("FolderOnly");
    }

    /// <summary>
    /// 验证 V1.1 MediaType 筛选：Image 只返回 image/* 文件，
    /// Video 只返回 video/* 文件，Other 返回非 image/video 文件。
    /// </summary>
    [Fact]
    public async Task Should_Filter_By_MediaType()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId,
                    "photo.jpg", size: 1000, contentType: "image/jpeg", blobName: "m/photo.jpg"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId,
                    "video.mp4", size: 50000, contentType: "video/mp4", blobName: "m/video.mp4"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId,
                    "document.pdf", size: 500, contentType: "application/pdf", blobName: "m/doc.pdf"));
        });

        // 筛选图片
        var images = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                mediaType: PrivateCloudDrive.FileCenter.FileCenterMediaTypeFilter.Image));

        images.Count.ShouldBe(1);
        images.Single().Name.ShouldBe("photo.jpg");

        // 筛选视频
        var videos = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                mediaType: PrivateCloudDrive.FileCenter.FileCenterMediaTypeFilter.Video));

        videos.Count.ShouldBe(1);
        videos.Single().Name.ShouldBe("video.mp4");

        // 筛选其他文件
        var others = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                mediaType: PrivateCloudDrive.FileCenter.FileCenterMediaTypeFilter.Other));

        others.Count.ShouldBe(1);
        others.Single().Name.ShouldBe("document.pdf");
    }

    /// <summary>
    /// 验证 V1.1 IsFavorite 筛选只返回收藏的节点。
    /// </summary>
    [Fact]
    public async Task Should_Filter_By_IsFavorite()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            // 使用 FileNode 的 SetFavorite 方法设置收藏状态
            var favoriteNode = PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                Guid.NewGuid(), tenantId: null, ownerId, parentId,
                "favorite.docx", size: 200, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document", blobName: "fav/fav.docx");
            favoriteNode.SetFavorite(true);
            await _fileNodeRepository.InsertAsync(favoriteNode);

            var normalNode = PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                Guid.NewGuid(), tenantId: null, ownerId, parentId,
                "normal.txt", size: 50, contentType: "text/plain", blobName: "fav/normal.txt");
            await _fileNodeRepository.InsertAsync(normalNode);
        });

        // 筛选仅收藏
        var favorites = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                isFavorite: true));

        favorites.Count.ShouldBe(1);
        favorites.Single().Name.ShouldBe("favorite.docx");
        favorites.Single().IsFavorite.ShouldBeTrue();
    }

    /// <summary>
    /// 验证 V1.1 TagId 筛选只返回打了指定标签的节点。
    /// 安全契约：标签查询也限制 TenantId + OwnerId。
    /// </summary>
    [Fact]
    public async Task Should_Filter_By_TagId()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var otherTagId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var taggedNode = PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                Guid.NewGuid(), tenantId: null, ownerId, parentId,
                "tagged.txt", size: 100, contentType: "text/plain", blobName: "tag/tagged.txt");
            await _fileNodeRepository.InsertAsync(taggedNode);

            var untaggedNode = PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                Guid.NewGuid(), tenantId: null, ownerId, parentId,
                "untagged.txt", size: 200, contentType: "text/plain", blobName: "tag/untagged.txt");
            await _fileNodeRepository.InsertAsync(untaggedNode);

            // 给 taggedNode 打上 tagId 标签
            await _fileNodeTagRepository.InsertAsync(
                new PrivateCloudDrive.FileCenter.FileNodeTag(
                    Guid.NewGuid(), tenantId: null, ownerId, taggedNode.Id, tagId));
        });

        // 按 tagId 筛选 → 只返回 tagged.txt
        var tagged = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                tagId: tagId));

        tagged.Count.ShouldBe(1);
        tagged.Single().Name.ShouldBe("tagged.txt");

        // 按不存在的标签筛选 → 空结果
        var noMatch = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                tagId: otherTagId));

        noMatch.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证 V1.1 未知排序值降级到默认排序（文件夹优先、名称升序），
    /// 不抛异常、不拼接原始字符串。
    /// </summary>
    [Fact]
    public async Task Should_Fallback_Unknown_Sorting_To_Default()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId,
                    "zeta.txt", size: 12, contentType: "text/plain", blobName: "sort/zeta.txt"));
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFolder(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId, "alpha"));
        });

        // 传入未知排序值 → 降级到默认
        // 验证：不抛异常
        var childrenDefaultSort = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                sorting: null));

        // 验证：未知排序值也和默认相同
        var childrenUnknownSort = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                sorting: "invalid_field_name asc"));

        // 验证：恶意排序值
        var childrenMaliciousSort = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10,
                sorting: "BlobName; DROP TABLE FileNode--"));

        // 三种情况都返回默认排序：文件夹在前
        childrenDefaultSort.Select(n => n.Name).ShouldBe(new[] { "alpha", "zeta.txt" });
        childrenUnknownSort.Select(n => n.Name).ShouldBe(new[] { "alpha", "zeta.txt" });
        childrenMaliciousSort.Select(n => n.Name).ShouldBe(new[] { "alpha", "zeta.txt" });
    }

    /// <summary>
    /// 验证搜索结果分页稳定：TotalCount 与分页项一致，
    /// 不同页码各自返回正确子集。
    /// </summary>
    [Fact]
    public async Task Should_Have_Consistent_TotalCount_With_Pagination()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            for (var i = 1; i <= 7; i++)
            {
                await _fileNodeRepository.InsertAsync(
                    PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                        Guid.NewGuid(), tenantId: null, ownerId, parentId,
                        $"file{i}.txt", size: i * 10, contentType: "text/plain", blobName: $"page/file{i}.txt"));
            }
        });

        // 第一页：每页 3 条
        var page1 = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 3));

        var totalCount = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenCountAsync(ownerId, parentId));

        totalCount.ShouldBe(7);
        page1.Count.ShouldBe(3);

        // 第二页
        var page2 = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 3, maxResultCount: 3));

        page2.Count.ShouldBe(3);

        // 第三页：最后一页只有 1 条
        var page3 = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 6, maxResultCount: 3));

        page3.Count.ShouldBe(1);

        // 总页数一致性：前三页数据不重复
        var allNames = page1.Concat(page2).Concat(page3).Select(n => n.Name).ToList();
        allNames.Distinct().Count().ShouldBe(7);
    }

    /// <summary>
    /// 验证搜索结果不暴露敏感信息：BlobName 是合法的存储键，
    /// 不包含连接串、密钥、内部路径遍历模式。
    /// </summary>
    [Fact]
    public async Task Should_Not_Expose_Sensitive_Info_In_Search_Results()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _fileNodeRepository.InsertAsync(
                PrivateCloudDrive.FileCenter.FileNode.CreateFile(
                    Guid.NewGuid(), tenantId: null, ownerId, parentId,
                    "safe.pdf", size: 100, contentType: "application/pdf",
                    blobName: "user-uploads/safe.pdf"));
        });

        var children = await WithUnitOfWorkAsync(async () =>
            await _fileNodeRepository.GetChildrenAsync(
                ownerId, parentId, skipCount: 0, maxResultCount: 10));

        var node = children.Single();

        // BlobName 是合法的存储键（路径格式），且不包含敏感关键词
        node.BlobName.ShouldNotBeNull();
        node.BlobName.ShouldNotContain("connection");
        node.BlobName.ShouldNotContain("password");
        node.BlobName.ShouldNotContain("secret");
        node.BlobName.ShouldNotContain("key=");
        node.BlobName.ShouldNotContain(".."); // 防止路径遍历
        node.BlobName.ShouldNotContain("\\"); // 防止 Windows 路径泄露
    }
}
