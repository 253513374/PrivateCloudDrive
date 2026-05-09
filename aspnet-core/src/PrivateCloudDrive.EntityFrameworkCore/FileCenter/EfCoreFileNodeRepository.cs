using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrivateCloudDrive.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 提供EfCoreFileNodeRepository持久化访问能力，封装查询条件和数据存取细节。
/// </summary>
public class EfCoreFileNodeRepository
    : EfCoreRepository<PrivateCloudDriveDbContext, FileNode, Guid>,
        IFileNodeRepository
{
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileNodeRepository"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileNodeRepository(
        IDbContextProvider<PrivateCloudDriveDbContext> dbContextProvider,
        IDataFilter<ISoftDelete> softDeleteFilter)
        : base(dbContextProvider)
    {
        _softDeleteFilter = softDeleteFilter;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<FileNode?> FindByNameAsync(
        Guid ownerId,
        Guid? parentId,
        string name,
        Guid? tenantId = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = FileNode.NormalizeName(name);

        async Task<FileNode?> QueryAsync()
        {
            return await (await GetQueryableAsync())
                .Where(node =>
                    node.TenantId == tenantId &&
                    node.OwnerId == ownerId &&
                    node.ParentId == parentId &&
                    node.NormalizedName == normalizedName)
                .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));
        }

        if (!includeDeleted)
        {
            return await QueryAsync();
        }

        using (_softDeleteFilter.Disable())
        {
            return await QueryAsync();
        }
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<FileNode?> FindByIdAsync(
        Guid id,
        Guid ownerId,
        Guid? tenantId = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        async Task<FileNode?> QueryAsync()
        {
            return await (await GetQueryableAsync())
                .Where(node =>
                    node.Id == id &&
                    node.TenantId == tenantId &&
                    node.OwnerId == ownerId)
                .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));
        }

        if (!includeDeleted)
        {
            return await QueryAsync();
        }

        using (_softDeleteFilter.Disable())
        {
            return await QueryAsync();
        }
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<List<FileNode>> GetChildrenAsync(
        Guid ownerId,
        Guid? parentId,
        int skipCount,
        int maxResultCount,
        Guid? tenantId = null,
        bool includeDeleted = false,
        Guid? tagId = null,
        bool? isFavorite = null,
        CancellationToken cancellationToken = default)
    {
        async Task<List<FileNode>> QueryAsync()
        {
            return await (await CreateChildrenQueryAsync(ownerId, parentId, tenantId, tagId, isFavorite))
                .OrderBy(node => node.NodeType)
                .ThenBy(node => node.NormalizedName)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        if (!includeDeleted)
        {
            return await QueryAsync();
        }

        using (_softDeleteFilter.Disable())
        {
            return await QueryAsync();
        }
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<long> GetChildrenCountAsync(
        Guid ownerId,
        Guid? parentId,
        Guid? tenantId = null,
        Guid? tagId = null,
        bool? isFavorite = null,
        CancellationToken cancellationToken = default)
    {
        return await (await CreateChildrenQueryAsync(ownerId, parentId, tenantId, tagId, isFavorite))
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }

    private async Task<IQueryable<FileNode>> CreateChildrenQueryAsync(
        Guid ownerId,
        Guid? parentId,
        Guid? tenantId,
        Guid? tagId,
        bool? isFavorite)
    {
        var queryable = (await GetQueryableAsync())
            .Where(node =>
                node.TenantId == tenantId &&
                node.OwnerId == ownerId &&
                node.ParentId == parentId);

        if (isFavorite.HasValue)
        {
            queryable = queryable.Where(node => node.IsFavorite == isFavorite.Value);
        }

        if (tagId.HasValue)
        {
            var dbContext = await GetDbContextAsync();
            var taggedNodeIds = dbContext.FileNodeTags
                .Where(nodeTag =>
                    nodeTag.TenantId == tenantId &&
                    nodeTag.OwnerId == ownerId &&
                    nodeTag.TagId == tagId.Value)
                .Select(nodeTag => nodeTag.FileNodeId);

            queryable = queryable.Where(node => taggedNodeIds.Contains(node.Id));
        }

        return queryable;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<List<FileNode>> GetDeletedRootsAsync(
        Guid ownerId,
        int skipCount,
        int maxResultCount,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        using (_softDeleteFilter.Disable())
        {
            var queryable = await GetQueryableAsync();

            return await queryable
                .Where(node =>
                    node.TenantId == tenantId &&
                    node.OwnerId == ownerId &&
                    node.IsDeleted &&
                    (
                        node.ParentId == null ||
                        !queryable.Any(parent =>
                            parent.Id == node.ParentId &&
                            parent.IsDeleted)
                    ))
                .OrderByDescending(node => node.DeletionTime)
                .ThenBy(node => node.NormalizedName)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<long> GetDeletedRootsCountAsync(
        Guid ownerId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        using (_softDeleteFilter.Disable())
        {
            var queryable = await GetQueryableAsync();

            return await queryable
                .LongCountAsync(
                    node =>
                        node.TenantId == tenantId &&
                        node.OwnerId == ownerId &&
                        node.IsDeleted &&
                        (
                            node.ParentId == null ||
                            !queryable.Any(parent =>
                                parent.Id == node.ParentId &&
                                parent.IsDeleted)
                        ),
                GetCancellationToken(cancellationToken));
        }
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    public async Task DeleteByIdDirectAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using (_softDeleteFilter.Disable())
        {
            await DeleteDirectAsync(node => node.Id == id, cancellationToken);
        }
    }
}
