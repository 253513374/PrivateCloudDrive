using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件夹和目录树应用服务。
/// 负责文件夹创建、列表、重命名、移动、回收站、恢复、永久删除和清空回收站等用户操作。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterFoldersAppService : FileCenterAppService, IFileCenterFoldersAppService
{
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly FileNodeManager _fileNodeManager;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly IBlobContainer<FileCenterBlobContainer> _blobContainer;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="FileCenterFoldersAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterFoldersAppService(
        IFileNodeRepository fileNodeRepository,
        FileNodeManager fileNodeManager,
        IRepository<BlobObject, Guid> blobObjectRepository,
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        IBlobContainer<FileCenterBlobContainer> blobContainer,
        IDataFilter<ISoftDelete> softDeleteFilter,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _fileNodeRepository = fileNodeRepository;
        _fileNodeManager = fileNodeManager;
        _blobObjectRepository = blobObjectRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _blobContainer = blobContainer;
        _softDeleteFilter = softDeleteFilter;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 在当前用户指定父目录下创建文件夹。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> CreateAsync(CreateFolderInput input)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.CreateFolderAsync(CurrentTenant.Id, ownerId, input.ParentId, input.Name);

        await _fileNodeRepository.InsertAsync(folder, autoSave: true);

        return ToDto(folder);
    }

    /// <summary>
    /// 查询指定文件夹的直接子节点；ParentId 为空时表示根目录。
    /// </summary>
    public virtual async Task<PagedResultDto<FileNodeDto>> GetListAsync(GetFolderChildrenInput input)
    {
        var ownerId = GetOwnerId();

        if (input.ParentId.HasValue)
        {
            await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, input.ParentId.Value);
        }

        var totalCount = await _fileNodeRepository.GetChildrenCountAsync(
            ownerId,
            input.ParentId,
            CurrentTenant.Id,
            input.TagId,
            input.IsFavorite);
        var items = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            input.ParentId,
            input.SkipCount,
            input.MaxResultCount,
            CurrentTenant.Id,
            tagId: input.TagId,
            isFavorite: input.IsFavorite);

        return new PagedResultDto<FileNodeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    /// <summary>
    /// 查询当前用户回收站中的节点列表。
    /// </summary>
    public virtual async Task<PagedResultDto<FileNodeDto>> GetDeletedListAsync(PagedResultRequestDto input)
    {
        var ownerId = GetOwnerId();
        var totalCount = await _fileNodeRepository.GetDeletedRootsCountAsync(ownerId, CurrentTenant.Id);
        var items = await _fileNodeRepository.GetDeletedRootsAsync(
            ownerId,
            input.SkipCount,
            input.MaxResultCount,
            CurrentTenant.Id);

        return new PagedResultDto<FileNodeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    /// <summary>
    /// 重命名资源，并校验同级名称唯一性和业务命名规则。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> RenameAsync(Guid id, RenameFileNodeInput input)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.RenameAsync(CurrentTenant.Id, ownerId, folder, input.Name);
        await _fileNodeRepository.UpdateAsync(folder, autoSave: true);

        return ToDto(folder);
    }

    /// <summary>
    /// 移动资源到新的业务位置，并校验层级关系、归属和命名约束。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> MoveAsync(Guid id, MoveFileNodeInput input)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.MoveAsync(CurrentTenant.Id, ownerId, folder, input.ParentId);
        await _fileNodeRepository.UpdateAsync(folder, autoSave: true);

        return ToDto(folder);
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.DeleteFolderTreeAsync(CurrentTenant.Id, ownerId, folder);
    }

    /// <summary>
    /// 从回收站恢复节点；恢复前会校验父目录状态和同名冲突。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> RestoreAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var node = await _fileNodeManager.GetOwnerDeletedNodeAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.RestoreTreeAsync(CurrentTenant.Id, ownerId, node);

        return ToDto(node);
    }

    /// <summary>
    /// 永久删除回收站节点及其子节点，删除后不可通过软删除恢复。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task PermanentDeleteAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var node = await _fileNodeManager.GetOwnerDeletedNodeAsync(CurrentTenant.Id, ownerId, id);
        var deletedNodes = await GetDeletedTreeNodesAsync(ownerId, node);

        await CleanupPermanentDeletedFilesAsync(ownerId, deletedNodes);
        await _fileNodeManager.PermanentDeleteTreeAsync(CurrentTenant.Id, ownerId, node);
    }

    /// <summary>
    /// 清空当前用户回收站中的所有节点。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task EmptyTrashAsync()
    {
        var ownerId = GetOwnerId();
        var deletedRoots = await _fileNodeRepository.GetDeletedRootsAsync(
            ownerId,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            CurrentTenant.Id);

        foreach (var deletedRoot in deletedRoots)
        {
            var deletedNodes = await GetDeletedTreeNodesAsync(ownerId, deletedRoot);

            await CleanupPermanentDeletedFilesAsync(ownerId, deletedNodes);
            await _fileNodeManager.PermanentDeleteTreeAsync(CurrentTenant.Id, ownerId, deletedRoot);
        }
    }

    private async Task<List<FileNode>> GetDeletedTreeNodesAsync(Guid ownerId, FileNode node)
    {
        var nodes = new List<FileNode> { node };
        var children = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            node.Id,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            tenantId: CurrentTenant.Id,
            includeDeleted: true);

        foreach (var child in children)
        {
            nodes.AddRange(await GetDeletedTreeNodesAsync(ownerId, child));
        }

        return nodes;
    }

    private async Task CleanupPermanentDeletedFilesAsync(Guid ownerId, IReadOnlyCollection<FileNode> deletedNodes)
    {
        var deletedNodeIds = deletedNodes.Select(node => node.Id).ToList();
        var mediaAssets = await _mediaAssetRepository.GetListAsync(
            asset =>
                asset.TenantId == CurrentTenant.Id &&
                asset.OwnerId == ownerId &&
                deletedNodeIds.Contains(asset.FileNodeId));

        foreach (var mediaAsset in mediaAssets)
        {
            await DeleteBlobObjectByIdAsync(ownerId, mediaAsset.ThumbnailBlobObjectId);
            await DeleteBlobObjectByIdAsync(ownerId, mediaAsset.PreviewBlobObjectId);
            await _mediaAssetRepository.DeleteDirectAsync(asset => asset.Id == mediaAsset.Id);
        }

        foreach (var fileNode in deletedNodes.Where(node => node.NodeType == FileNodeType.File))
        {
            if (string.IsNullOrWhiteSpace(fileNode.BlobName))
            {
                continue;
            }

            if (await IsBlobReferencedOutsideDeletedNodesAsync(ownerId, fileNode.BlobName, deletedNodeIds))
            {
                continue;
            }

            var blobObject = await _blobObjectRepository.FirstOrDefaultAsync(
                blob =>
                    blob.TenantId == CurrentTenant.Id &&
                    blob.OwnerId == ownerId &&
                    blob.BlobName == fileNode.BlobName);

            await DeleteBlobObjectAsync(blobObject);
        }
    }

    private async Task<bool> IsBlobReferencedOutsideDeletedNodesAsync(
        Guid ownerId,
        string blobName,
        IReadOnlyCollection<Guid> deletedNodeIds)
    {
        using (_softDeleteFilter.Disable())
        {
            var queryable = await _fileNodeRepository.GetQueryableAsync();
            var referencedNodeIds = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(node =>
                        node.TenantId == CurrentTenant.Id &&
                        node.OwnerId == ownerId &&
                        node.BlobName == blobName)
                    .Select(node => node.Id));

            return referencedNodeIds.Any(id => !deletedNodeIds.Contains(id));
        }
    }

    private async Task DeleteBlobObjectByIdAsync(Guid ownerId, Guid? blobObjectId)
    {
        if (!blobObjectId.HasValue)
        {
            return;
        }

        var blobObject = await _blobObjectRepository.FirstOrDefaultAsync(
            blob =>
                blob.Id == blobObjectId.Value &&
                blob.TenantId == CurrentTenant.Id &&
                blob.OwnerId == ownerId);

        await DeleteBlobObjectAsync(blobObject);
    }

    private async Task DeleteBlobObjectAsync(BlobObject? blobObject)
    {
        if (blobObject == null)
        {
            return;
        }

        await _blobContainer.DeleteAsync(blobObject.BlobName);
        await _blobObjectRepository.DeleteDirectAsync(blob => blob.Id == blobObject.Id);
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return CurrentUser.Id.Value;
    }

    private static FileNodeDto ToDto(FileNode node)
    {
        return new FileNodeDto
        {
            Id = node.Id,
            TenantId = node.TenantId,
            OwnerId = node.OwnerId,
            ParentId = node.ParentId,
            NodeType = node.NodeType,
            Name = node.Name,
            NormalizedName = node.NormalizedName,
            Size = node.Size,
            ContentType = node.ContentType,
            BlobName = node.BlobName,
            IsFavorite = node.IsFavorite,
            CreationTime = node.CreationTime,
            LastModificationTime = node.LastModificationTime
        };
    }
}
