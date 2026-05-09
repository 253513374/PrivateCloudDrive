using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 提供EfCoreFileNodeRepositoryTests持久化访问能力，封装查询条件和数据存取细节。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileNodeRepositoryTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileNodeRepository _fileNodeRepository;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileNodeRepositoryTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileNodeRepositoryTests()
    {
        _fileNodeRepository = GetRequiredService<PrivateCloudDrive.FileCenter.IFileNodeRepository>();
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
}
