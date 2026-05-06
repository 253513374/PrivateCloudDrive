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

public class EfCoreFileNodeRepository
    : EfCoreRepository<PrivateCloudDriveDbContext, FileNode, Guid>,
        IFileNodeRepository
{
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

    public EfCoreFileNodeRepository(
        IDbContextProvider<PrivateCloudDriveDbContext> dbContextProvider,
        IDataFilter<ISoftDelete> softDeleteFilter)
        : base(dbContextProvider)
    {
        _softDeleteFilter = softDeleteFilter;
    }

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

    public async Task<List<FileNode>> GetChildrenAsync(
        Guid ownerId,
        Guid? parentId,
        int skipCount,
        int maxResultCount,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync())
            .Where(node =>
                node.TenantId == tenantId &&
                node.OwnerId == ownerId &&
                node.ParentId == parentId)
            .OrderBy(node => node.NodeType)
            .ThenBy(node => node.NormalizedName)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetChildrenCountAsync(
        Guid ownerId,
        Guid? parentId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync())
            .LongCountAsync(
                node =>
                    node.TenantId == tenantId &&
                    node.OwnerId == ownerId &&
                    node.ParentId == parentId,
                GetCancellationToken(cancellationToken));
    }
}
