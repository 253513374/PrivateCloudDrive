using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.Deployment;

namespace PrivateCloudDrive.Controllers.Deployment;

/// <summary>
/// 部署健康检查 HTTP API 控制器。允许匿名访问，供运维人员部署后确认系统就绪。
/// 所有输出均不包含密码、token、OAuth code、client secret 或完整私有 URL。
/// </summary>
[Route("api/health")]
[AllowAnonymous]
public class DeploymentHealthController : PrivateCloudDriveController
{
    private readonly IDeploymentHealthCheckService _deploymentHealthCheckService;

    /// <summary>
    /// 初始化 <see cref="DeploymentHealthController"/> 的新实例。
    /// </summary>
    public DeploymentHealthController(IDeploymentHealthCheckService deploymentHealthCheckService)
    {
        _deploymentHealthCheckService = deploymentHealthCheckService;
    }

    /// <summary>
    /// 获取部署健康检查完整结果，包含所有组件的 Pass/Warn/Fail 状态和修复建议。
    /// 无认证要求，部署后即可通过此端点确认系统是否就绪。
    /// </summary>
    /// <returns>部署健康检查结果。</returns>
    [HttpGet]
    public virtual async Task<DeploymentHealthDto> GetAsync()
    {
        return await _deploymentHealthCheckService.GetHealthAsync();
    }
}
