using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;

namespace PrivateCloudDrive.FileCenter;

[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterFoldersAppService : FileCenterAppService, IFileCenterFoldersAppService
{
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly FileNodeManager _fileNodeManager;

    public FileCenterFoldersAppService(
        IFileNodeRepository fileNodeRepository,
        FileNodeManager fileNodeManager)
    {
        _fileNodeRepository = fileNodeRepository;
        _fileNodeManager = fileNodeManager;
    }

    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> CreateAsync(CreateFolderInput input)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.CreateFolderAsync(CurrentTenant.Id, ownerId, input.ParentId, input.Name);

        await _fileNodeRepository.InsertAsync(folder, autoSave: true);

        return ToDto(folder);
    }

    public virtual async Task<PagedResultDto<FileNodeDto>> GetListAsync(GetFolderChildrenInput input)
    {
        var ownerId = GetOwnerId();

        if (input.ParentId.HasValue)
        {
            await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, input.ParentId.Value);
        }

        var totalCount = await _fileNodeRepository.GetChildrenCountAsync(ownerId, input.ParentId, CurrentTenant.Id);
        var items = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            input.ParentId,
            input.SkipCount,
            input.MaxResultCount,
            CurrentTenant.Id);

        return new PagedResultDto<FileNodeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> RenameAsync(Guid id, RenameFileNodeInput input)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.RenameAsync(CurrentTenant.Id, ownerId, folder, input.Name);
        await _fileNodeRepository.UpdateAsync(folder, autoSave: true);

        return ToDto(folder);
    }

    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<FileNodeDto> MoveAsync(Guid id, MoveFileNodeInput input)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.MoveAsync(CurrentTenant.Id, ownerId, folder, input.ParentId);
        await _fileNodeRepository.UpdateAsync(folder, autoSave: true);

        return ToDto(folder);
    }

    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var folder = await _fileNodeManager.GetOwnerFolderAsync(CurrentTenant.Id, ownerId, id);

        await _fileNodeManager.DeleteFolderTreeAsync(CurrentTenant.Id, ownerId, folder);
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
            CreationTime = node.CreationTime,
            LastModificationTime = node.LastModificationTime
        };
    }
}
