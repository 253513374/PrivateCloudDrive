using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 文件节点通用 HTTP API 控制器。
/// 当前主要承载节点删除、恢复和永久删除等跨文件/文件夹的操作。
/// </summary>
[Route("api/file-center/nodes")]
[Authorize]
public class FileCenterNodesController : PrivateCloudDriveController
{
    private readonly IFileCenterFoldersAppService _foldersAppService;
    private readonly IFileCenterFileUploadService _fileUploadService;

    /// <summary>
    /// 初始化 <see cref="FileCenterNodesController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterNodesController(
        IFileCenterFoldersAppService foldersAppService,
        IFileCenterFileUploadService fileUploadService)
    {
        _foldersAppService = foldersAppService;
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// 根据节点类型删除文件或文件夹；文件夹会递归软删除整棵子树。
    /// </summary>
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

    /// <summary>
    /// 从回收站恢复指定节点。
    /// </summary>
    [HttpPost("{id}/restore")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<FileNodeDto> RestoreAsync(Guid id)
    {
        return _foldersAppService.RestoreAsync(id);
    }

    /// <summary>
    /// 永久删除回收站节点，删除后不可恢复。
    /// </summary>
    [HttpDelete("{id}/permanent")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual Task PermanentDeleteAsync(Guid id)
    {
        return _foldersAppService.PermanentDeleteAsync(id);
    }
}
