using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件节点领域服务，集中处理目录树规则和跨聚合校验。
/// 应用层在创建、移动、删除、恢复、永久删除节点时应优先通过该服务，避免绕过所有权、重名和目录循环校验。
/// </summary>
public class FileNodeManager : FileCenterDomainService
{
    private readonly IFileNodeRepository _fileNodeRepository;

    /// <summary>
    /// 初始化 <see cref="FileNodeManager"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileNodeManager(IFileNodeRepository fileNodeRepository)
    {
        _fileNodeRepository = fileNodeRepository;
    }

    /// <summary>
    /// 创建文件夹前校验父目录归属和同目录名称唯一性。
    /// </summary>
    public virtual async Task<FileNode> CreateFolderAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name)
    {
        await EnsureCanCreateAsync(tenantId, ownerId, parentId, name);

        return FileNode.CreateFolder(
            GuidGenerator.Create(),
            tenantId,
            ownerId,
            parentId,
            name);
    }

    /// <summary>
    /// 创建文件节点前校验父目录归属和同目录名称唯一性。
    /// </summary>
    public virtual async Task<FileNode> CreateFileAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name,
        long size,
        string? contentType,
        string blobName)
    {
        await EnsureCanCreateAsync(tenantId, ownerId, parentId, name);

        return FileNode.CreateFile(
            GuidGenerator.Create(),
            tenantId,
            ownerId,
            parentId,
            name,
            size,
            contentType,
            blobName);
    }

    /// <summary>
    /// 校验指定父目录下是否允许创建新节点。
    /// </summary>
    public virtual async Task EnsureCanCreateAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name)
    {
        await EnsureParentFolderExistsAsync(tenantId, ownerId, parentId);
        await EnsureNameNotExistsAsync(tenantId, ownerId, parentId, name);
    }

    /// <summary>
    /// 重命名节点，并阻止与同一父目录下其他节点重名。
    /// </summary>
    public virtual async Task RenameAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node,
        string name)
    {
        EnsureOwnerNode(tenantId, ownerId, node);
        EnsureFolderNode(node);

        var existingNode = await _fileNodeRepository.FindByNameAsync(
            ownerId,
            node.ParentId,
            name,
            tenantId);

        if (existingNode != null && existingNode.Id != node.Id)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists)
                .WithData("Name", name);
        }

        node.Rename(name);
    }

    /// <summary>
    /// 移动节点到目标目录，同时校验所有权、目标目录、目录循环和重名冲突。
    /// </summary>
    public virtual async Task MoveAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node,
        Guid? parentId)
    {
        EnsureOwnerNode(tenantId, ownerId, node);
        EnsureFolderNode(node);
        await MoveNodeAsync(tenantId, ownerId, node, parentId);
    }

    /// <summary>
    /// 移动文件或文件夹节点到目标目录；文件夹移动时额外阻止移动到自身或子孙目录。
    /// </summary>
    public virtual async Task MoveNodeAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node,
        Guid? parentId)
    {
        EnsureOwnerNode(tenantId, ownerId, node);
        await EnsureCanMoveAsync(tenantId, ownerId, node, parentId);

        var existingNode = await _fileNodeRepository.FindByNameAsync(
            ownerId,
            parentId,
            node.Name,
            tenantId);

        if (existingNode != null && existingNode.Id != node.Id)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists)
                .WithData("Name", node.Name);
        }

        node.MoveTo(parentId);
    }

    /// <summary>
    /// 递归软删除文件夹树，使文件夹及其子节点进入回收站。
    /// </summary>
    public virtual async Task DeleteFolderTreeAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node)
    {
        EnsureOwnerNode(tenantId, ownerId, node);
        EnsureFolderNode(node);

        var children = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            node.Id,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            tenantId);

        foreach (var child in children)
        {
            if (child.NodeType == FileNodeType.Folder)
            {
                await DeleteFolderTreeAsync(tenantId, ownerId, child);
            }
            else
            {
                await _fileNodeRepository.DeleteAsync(child);
            }
        }

        await _fileNodeRepository.DeleteAsync(node);
    }

    /// <summary>
    /// 从回收站递归恢复节点树；恢复前会校验父目录仍存在且未与现有节点重名。
    /// </summary>
    public virtual async Task RestoreTreeAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node)
    {
        EnsureOwnerNode(tenantId, ownerId, node);

        if (!node.IsDeleted)
        {
            return;
        }

        await EnsureCanRestoreAsync(tenantId, ownerId, node);
        await RestoreTreeCoreAsync(tenantId, ownerId, node);
    }

    /// <summary>
    /// 递归永久删除节点树，直接移除数据库记录。
    /// 调用方应确保底层 Blob 清理策略已经明确，避免元数据和文件内容生命周期不一致。
    /// </summary>
    public virtual async Task PermanentDeleteTreeAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node)
    {
        EnsureOwnerNode(tenantId, ownerId, node);

        var children = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            node.Id,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            tenantId,
            includeDeleted: true);

        foreach (var child in children)
        {
            await PermanentDeleteTreeAsync(tenantId, ownerId, child);
        }

        await _fileNodeRepository.DeleteByIdDirectAsync(node.Id);
    }

    /// <summary>
    /// 获取当前用户拥有的文件夹节点；不存在、跨租户或跨用户时统一按未找到处理。
    /// </summary>
    public virtual async Task<FileNode> GetOwnerFolderAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid id)
    {
        var node = await _fileNodeRepository.FindAsync(id);

        if (node == null || node.TenantId != tenantId || node.OwnerId != ownerId)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", id);
        }

        EnsureFolderNode(node);

        return node;
    }

    /// <summary>
    /// 获取当前用户拥有的文件节点；用于下载、预览和删除等文件专属操作。
    /// </summary>
    public virtual async Task<FileNode> GetOwnerFileAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid id)
    {
        var node = await _fileNodeRepository.FindAsync(id);

        if (node == null || node.TenantId != tenantId || node.OwnerId != ownerId)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", id);
        }

        EnsureFileNode(node);

        return node;
    }

    /// <summary>
    /// 获取当前用户拥有的任意文件中心节点；用于批量操作统一处理文件和文件夹。
    /// </summary>
    public virtual async Task<FileNode> GetOwnerNodeAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid id,
        bool includeDeleted = false)
    {
        var node = await _fileNodeRepository.FindByIdAsync(id, ownerId, tenantId, includeDeleted);

        if (node == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", id);
        }

        return node;
    }

    /// <summary>
    /// 获取当前用户回收站中的节点，供恢复和永久删除入口使用。
    /// </summary>
    public virtual async Task<FileNode> GetOwnerDeletedNodeAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid id)
    {
        var node = await _fileNodeRepository.FindByIdAsync(id, ownerId, tenantId, includeDeleted: true);

        if (node == null || !node.IsDeleted)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", id);
        }

        return node;
    }

    private async Task RestoreTreeCoreAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node)
    {
        node.Restore();
        await _fileNodeRepository.UpdateAsync(node);

        var children = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            node.Id,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            tenantId,
            includeDeleted: true);

        foreach (var child in children.Where(child => child.IsDeleted))
        {
            await RestoreTreeCoreAsync(tenantId, ownerId, child);
        }
    }

    /// <summary>
    /// 校验回收站节点是否可恢复：父目录必须存在且未删除，目标目录不能出现同名节点。
    /// </summary>
    private async Task EnsureCanRestoreAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node)
    {
        if (node.ParentId.HasValue)
        {
            var parent = await _fileNodeRepository.FindByIdAsync(
                node.ParentId.Value,
                ownerId,
                tenantId,
                includeDeleted: true);

            if (parent == null || parent.IsDeleted)
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterParentFolderNotFound)
                    .WithData("ParentId", node.ParentId);
            }
        }

        var existingNode = await _fileNodeRepository.FindByNameAsync(
            ownerId,
            node.ParentId,
            node.Name,
            tenantId);

        if (existingNode != null && existingNode.Id != node.Id)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists)
                .WithData("Name", node.Name);
        }
    }

    /// <summary>
    /// 校验目录移动不会形成“移动到自身或子孙目录”的循环结构。
    /// </summary>
    private async Task EnsureCanMoveAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node,
        Guid? parentId)
    {
        if (parentId == null)
        {
            return;
        }

        var currentParent = await GetOwnerFolderAsync(tenantId, ownerId, parentId.Value);

        while (currentParent != null)
        {
            if (currentParent.Id == node.Id)
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterCannotMoveToSelfOrDescendant)
                    .WithData("Id", node.Id)
                    .WithData("ParentId", parentId);
            }

            if (currentParent.ParentId == null)
            {
                break;
            }

            currentParent = await GetOwnerFolderAsync(tenantId, ownerId, currentParent.ParentId.Value);
        }
    }

    private async Task EnsureParentFolderExistsAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId)
    {
        if (parentId == null)
        {
            return;
        }

        var parent = await _fileNodeRepository.FindAsync(parentId.Value);

        if (parent == null ||
            parent.TenantId != tenantId ||
            parent.OwnerId != ownerId ||
            parent.NodeType != FileNodeType.Folder)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterParentFolderNotFound)
                .WithData("ParentId", parentId);
        }
    }

    private async Task EnsureNameNotExistsAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name)
    {
        var existingNode = await _fileNodeRepository.FindByNameAsync(
            ownerId,
            parentId,
            name,
            tenantId);

        if (existingNode != null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists)
                .WithData("Name", name);
        }
    }

    private static void EnsureOwnerNode(
        Guid? tenantId,
        Guid ownerId,
        FileNode node)
    {
        if (node.TenantId != tenantId || node.OwnerId != ownerId)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", node.Id);
        }
    }

    private static void EnsureFolderNode(FileNode node)
    {
        if (node.NodeType != FileNodeType.Folder)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyFolderCanBeManaged)
                .WithData("Id", node.Id);
        }
    }

    private static void EnsureFileNode(FileNode node)
    {
        if (node.NodeType != FileNodeType.File)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyFileCanBeDownloaded)
                .WithData("Id", node.Id);
        }
    }
}
