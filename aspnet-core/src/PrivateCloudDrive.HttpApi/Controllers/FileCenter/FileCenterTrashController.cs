using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/trash")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterTrashController : PrivateCloudDriveController
{
    private readonly IFileCenterFoldersAppService _foldersAppService;

    public FileCenterTrashController(IFileCenterFoldersAppService foldersAppService)
    {
        _foldersAppService = foldersAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<FileNodeDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _foldersAppService.GetDeletedListAsync(input);
    }

    [HttpDelete]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual Task EmptyAsync()
    {
        return _foldersAppService.EmptyTrashAsync();
    }
}
