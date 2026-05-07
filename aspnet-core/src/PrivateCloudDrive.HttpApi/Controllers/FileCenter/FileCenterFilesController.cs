using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Models.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/files")]
[Authorize]
public class FileCenterFilesController : PrivateCloudDriveController
{
    private readonly IFileCenterFileUploadService _fileUploadService;
    private readonly IFileCenterFileDownloadService _fileDownloadService;

    public FileCenterFilesController(
        IFileCenterFileUploadService fileUploadService,
        IFileCenterFileDownloadService fileDownloadService)
    {
        _fileUploadService = fileUploadService;
        _fileDownloadService = fileDownloadService;
    }

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

    [HttpGet("{id}/thumbnail")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
    public virtual async Task<IActionResult> ThumbnailAsync(Guid id)
    {
        var file = await _fileDownloadService.GetThumbnailAsync(id, HttpContext.RequestAborted);

        return new FileStreamResult(file.Content, file.ContentType);
    }

    [HttpDelete("{id}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual async Task<IActionResult> DeleteAsync(Guid id)
    {
        await _fileUploadService.DeleteAsync(id, HttpContext.RequestAborted);

        return NoContent();
    }
}
