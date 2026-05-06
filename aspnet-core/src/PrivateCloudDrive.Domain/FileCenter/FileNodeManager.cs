using System;
using System.Threading.Tasks;
using Volo.Abp;

namespace PrivateCloudDrive.FileCenter;

public class FileNodeManager : FileCenterDomainService
{
    private readonly IFileNodeRepository _fileNodeRepository;

    public FileNodeManager(IFileNodeRepository fileNodeRepository)
    {
        _fileNodeRepository = fileNodeRepository;
    }

    public virtual async Task<FileNode> CreateFolderAsync(
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name)
    {
        await EnsureParentFolderExistsAsync(tenantId, ownerId, parentId);
        await EnsureNameNotExistsAsync(tenantId, ownerId, parentId, name);

        return FileNode.CreateFolder(
            GuidGenerator.Create(),
            tenantId,
            ownerId,
            parentId,
            name);
    }

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

    public virtual async Task MoveAsync(
        Guid? tenantId,
        Guid ownerId,
        FileNode node,
        Guid? parentId)
    {
        EnsureOwnerNode(tenantId, ownerId, node);
        EnsureFolderNode(node);
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
}
