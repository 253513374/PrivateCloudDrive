using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 管理员级别的系统健康 HTTP API 控制器。
/// </summary>
[Route("api/admin/system-health")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class AdminSystemHealthController : PrivateCloudDriveController
{
    private readonly IFileCenterSystemHealthAppService _systemHealthAppService;

    /// <summary>
    /// 初始化 <see cref="AdminSystemHealthController"/> 的新实例。
    /// </summary>
    public AdminSystemHealthController(IFileCenterSystemHealthAppService systemHealthAppService)
    {
        _systemHealthAppService = systemHealthAppService;
    }

    /// <summary>
    /// 获取管理员级别的系统健康全局视图。
    /// </summary>
    [HttpGet("summary")]
    public virtual Task<AdminFileCenterSystemHealthDto> GetAdminSummaryAsync()
    {
        return _systemHealthAppService.GetAdminSummaryAsync();
    }
}
