using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/nodes")]
[Authorize]
public class FileCenterNodesController : PrivateCloudDriveController
{
    private readonly IFileCenterFoldersAppService _foldersAppService;
    private readonly IFileCenterFileUploadService _fileUploadService;

    public FileCenterNodesController(
        IFileCenterFoldersAppService foldersAppService,
        IFileCenterFileUploadService fileUploadService)
    {
        _foldersAppService = foldersAppService;
        _fileUploadService = fileUploadService;
    }

    [HttpDelete("{id}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _fileUploadService.DeleteAsync(id, cancellationToken);
        }
        catch (BusinessException exception) when (
            exception.Code == PrivateCloudDriveDomainErrorCodes.FileCenterOnlyFileCanBeDownloaded ||
            exception.Code == PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
        {
            await _foldersAppService.DeleteAsync(id);
        }

        return NoContent();
    }

    [HttpPost("{id}/restore")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<FileNodeDto> RestoreAsync(Guid id)
    {
        return _foldersAppService.RestoreAsync(id);
    }

    [HttpDelete("{id}/permanent")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual Task PermanentDeleteAsync(Guid id)
    {
        return _foldersAppService.PermanentDeleteAsync(id);
    }
}
