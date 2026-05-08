using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/media")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterMediaLibraryController : PrivateCloudDriveController
{
    private readonly IFileCenterMediaLibraryAppService _mediaLibraryAppService;

    public FileCenterMediaLibraryController(IFileCenterMediaLibraryAppService mediaLibraryAppService)
    {
        _mediaLibraryAppService = mediaLibraryAppService;
    }

    [HttpGet("images")]
    public virtual Task<PagedResultDto<FileNodeDto>> GetImagesAsync([FromQuery] GetMediaFilesInput input)
    {
        return _mediaLibraryAppService.GetImagesAsync(input);
    }

    [HttpGet("videos")]
    public virtual Task<PagedResultDto<FileNodeDto>> GetVideosAsync([FromQuery] GetMediaFilesInput input)
    {
        return _mediaLibraryAppService.GetVideosAsync(input);
    }
}
