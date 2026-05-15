using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 文件中心系统健康 HTTP API 控制器。
/// </summary>
[Route("api/file-center/system-health")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterSystemHealthController : PrivateCloudDriveController
{
    private readonly IFileCenterSystemHealthAppService _systemHealthAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterSystemHealthController"/> 的新实例。
    /// </summary>
    public FileCenterSystemHealthController(IFileCenterSystemHealthAppService systemHealthAppService)
    {
        _systemHealthAppService = systemHealthAppService;
    }

    /// <summary>
    /// 获取当前用户可见的系统健康摘要。
    /// </summary>
    [HttpGet("summary")]
    public virtual Task<FileCenterSystemHealthDto> GetSummaryAsync()
    {
        return _systemHealthAppService.GetSummaryAsync();
    }
}
