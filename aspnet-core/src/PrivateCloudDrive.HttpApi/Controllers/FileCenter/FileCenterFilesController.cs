using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Models.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/files")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Upload)]
public class FileCenterFilesController : PrivateCloudDriveController
{
    private readonly IFileCenterFileUploadService _fileUploadService;

    public FileCenterFilesController(IFileCenterFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    [HttpPost("upload-small")]
    [Consumes("multipart/form-data")]
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
}
