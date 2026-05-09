using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 登录用户分享管理 HTTP API 控制器。
/// </summary>
[Route("api/file-center/shares")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Share)]
public class FileCenterSharesController : PrivateCloudDriveController
{
    private readonly IFileCenterSharesAppService _sharesAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterSharesController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterSharesController(IFileCenterSharesAppService sharesAppService)
    {
        _sharesAppService = sharesAppService;
    }

    /// <summary>
    /// 创建文件或文件夹分享链接。
    /// </summary>
    [HttpPost]
    public virtual Task<FileShareDto> CreateAsync([FromBody] CreateFileShareInput input)
    {
        return _sharesAppService.CreateAsync(input);
    }

    /// <summary>
    /// 查询分页列表数据，并按当前用户、租户和输入条件进行过滤。
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<FileShareDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _sharesAppService.GetListAsync(input);
    }

    /// <summary>
    /// 管理员分页查看当前租户的全部分享。
    /// </summary>
    [HttpGet("all")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<PagedResultDto<FileShareDto>> GetAllListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _sharesAppService.GetAllListAsync(input);
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _sharesAppService.DeleteAsync(id);
    }

    /// <summary>
    /// 执行Disable操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    [HttpDelete("all/{id:guid}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task DisableAsync(Guid id)
    {
        return _sharesAppService.DisableAsync(id);
    }
}
