using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 管理员媒体处理任务 HTTP API 控制器。
/// </summary>
[Route("api/admin/media-tasks")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class MediaTasksController : PrivateCloudDriveController
{
    private readonly IMediaTasksAppService _mediaTasksAppService;

    /// <summary>
    /// 初始化 <see cref="MediaTasksController"/> 的新实例。
    /// </summary>
    public MediaTasksController(IMediaTasksAppService mediaTasksAppService)
    {
        _mediaTasksAppService = mediaTasksAppService;
    }

    /// <summary>
    /// 获取媒体处理任务分页列表。
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<MediaTaskDto>> GetListAsync([FromQuery] GetMediaTasksInput input)
    {
        return _mediaTasksAppService.GetListAsync(input);
    }

    /// <summary>
    /// 重新处理失败的任务。
    /// </summary>
    [HttpPost("{mediaAssetId}/retry")]
    public virtual async Task<IActionResult> RetryAsync(Guid mediaAssetId)
    {
        await _mediaTasksAppService.RetryAsync(mediaAssetId);
        return Ok();
    }
}
