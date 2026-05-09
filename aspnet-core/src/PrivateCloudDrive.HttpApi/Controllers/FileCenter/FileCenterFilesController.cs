using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Models.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 文件上传、下载、预览和删除 HTTP API 控制器。
/// 下载类接口通过应用服务校验当前用户权限后返回文件流。
/// </summary>
[Route("api/file-center/files")]
[Authorize]
public class FileCenterFilesController : PrivateCloudDriveController
{
    private readonly IFileCenterFileUploadService _fileUploadService;
    private readonly IFileCenterFileDownloadService _fileDownloadService;

    /// <summary>
    /// 初始化 <see cref="FileCenterFilesController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterFilesController(
        IFileCenterFileUploadService fileUploadService,
        IFileCenterFileDownloadService fileDownloadService)
    {
        _fileUploadService = fileUploadService;
        _fileDownloadService = fileDownloadService;
    }

    /// <summary>
    /// 小文件直传入口，适合一次请求完成上传的文件。
    /// </summary>
    [HttpPost("upload-small")]
    [Consumes("multipart/form-data")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Upload)]
    public virtual async Task<FileNodeDto> UploadSmallAsync([FromForm] UploadSmallFileForm input)
    {
        await using var stream = input.File.OpenReadStream();

        return await _fileUploadService.UploadSmallFileAsync(
            input.ParentId,
            input.File.FileName,
            input.File.ContentType,
            stream,
            input.File.Length,
            HttpContext.RequestAborted);
    }

    /// <summary>
    /// 下载原始文件。启用 RangeProcessing 以支持断点下载和视频拖动。
    /// </summary>
    [HttpGet("{id}/download")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Download)]
    public virtual async Task<IActionResult> DownloadAsync(Guid id)
    {
        var file = await _fileDownloadService.GetDownloadAsync(id, HttpContext.RequestAborted);

        return new FileStreamResult(file.Content, file.ContentType)
        {
            EnableRangeProcessing = true,
            FileDownloadName = file.FileName
        };
    }

    /// <summary>
    /// 返回浏览器可内嵌预览的文件内容。
    /// </summary>
    [HttpGet("{id}/content")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Download)]
    public virtual async Task<IActionResult> ContentAsync(Guid id)
    {
        var file = await _fileDownloadService.GetDownloadAsync(id, HttpContext.RequestAborted);

        return new FileStreamResult(file.Content, file.ContentType)
        {
            EnableRangeProcessing = true
        };
    }

    /// <summary>
    /// 返回图片或视频的缩略图内容。
    /// </summary>
    [HttpGet("{id}/thumbnail")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
    public virtual async Task<IActionResult> ThumbnailAsync(Guid id)
    {
        var file = await _fileDownloadService.GetThumbnailAsync(id, HttpContext.RequestAborted);

        return new FileStreamResult(file.Content, file.ContentType);
    }

    /// <summary>
    /// 删除文件节点，使其进入回收站。
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task<IActionResult> DeleteAsync(Guid id)
    {
        await _fileUploadService.DeleteAsync(id, HttpContext.RequestAborted);

        return NoContent();
    }
}
