using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/shares")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Share)]
public class FileCenterSharesController : PrivateCloudDriveController
{
    private readonly IFileCenterSharesAppService _sharesAppService;

    public FileCenterSharesController(IFileCenterSharesAppService sharesAppService)
    {
        _sharesAppService = sharesAppService;
    }

    [HttpPost]
    public virtual Task<FileShareDto> CreateAsync([FromBody] CreateFileShareInput input)
    {
        return _sharesAppService.CreateAsync(input);
    }

    [HttpGet]
    public virtual Task<PagedResultDto<FileShareDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _sharesAppService.GetListAsync(input);
    }

    [HttpGet("all")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<PagedResultDto<FileShareDto>> GetAllListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _sharesAppService.GetAllListAsync(input);
    }

    [HttpDelete("{id:guid}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _sharesAppService.DeleteAsync(id);
    }

    [HttpDelete("all/{id:guid}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task DisableAsync(Guid id)
    {
        return _sharesAppService.DisableAsync(id);
    }
}
