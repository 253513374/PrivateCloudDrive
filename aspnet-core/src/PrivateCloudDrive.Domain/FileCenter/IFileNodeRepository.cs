using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 提供IFileNodeRepository持久化访问能力，封装查询条件和数据存取细节。
/// </summary>
public interface IFileNodeRepository : IRepository<FileNode, Guid>
{
    Task<FileNode?> FindByNameAsync(
        Guid ownerId,
        Guid? parentId,
        string name,
        Guid? tenantId = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<FileNode?> FindByIdAsync(
        Guid id,
        Guid ownerId,
        Guid? tenantId = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<List<FileNode>> GetChildrenAsync(
        Guid ownerId,
        Guid? parentId,
        int skipCount,
        int maxResultCount,
        Guid? tenantId = null,
        bool includeDeleted = false,
        Guid? tagId = null,
        bool? isFavorite = null,
        CancellationToken cancellationToken = default);

    Task<long> GetChildrenCountAsync(
        Guid ownerId,
        Guid? parentId,
        Guid? tenantId = null,
        Guid? tagId = null,
        bool? isFavorite = null,
        CancellationToken cancellationToken = default);

    Task<List<FileNode>> GetDeletedRootsAsync(
        Guid ownerId,
        int skipCount,
        int maxResultCount,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task<long> GetDeletedRootsCountAsync(
        Guid ownerId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task DeleteByIdDirectAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
