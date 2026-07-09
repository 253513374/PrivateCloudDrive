using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.Deployment;

namespace PrivateCloudDrive.Controllers.Deployment;

/// <summary>
/// 部署健康检查 HTTP API 控制器。提供分层健康检查：
/// - /health/live: 仅进程存活，无需认证
/// - /health/ready: 低敏依赖就绪，无需认证
/// - /health/detail: 管理员全量详情，需要 admin 角色
/// 所有输出均不包含密码、token、OAuth code、client secret 或完整私有 URL。
/// </summary>
[Route("api/health")]
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
    /// 获取进程存活状态。极简探针，不依赖任何外部服务。
    /// 适用于负载均衡器和编排平台的存活探针。
    /// </summary>
    /// <returns>存活状态。</returns>
    [HttpGet("live")]
    [AllowAnonymous]
    public virtual async Task<DeploymentLiveDto> GetLiveAsync()
    {
        return await _deploymentHealthCheckService.GetLiveAsync();
    }

    /// <summary>
    /// 获取部署就绪状态。检查关键依赖（DB、Redis、存储）是否可用。
    /// 仅返回低敏状态，不含修复建议或敏感详情。
    /// 适用于编排平台就绪探针。
    /// </summary>
    /// <returns>就绪检查结果。</returns>
    [HttpGet("ready")]
    [AllowAnonymous]
    public virtual async Task<DeploymentReadyDto> GetReadyAsync()
    {
        return await _deploymentHealthCheckService.GetReadyAsync();
    }

    /// <summary>
    /// 获取部署健康检查完整结果，包含所有组件的 Pass/Warn/Fail 状态和修复建议。
    /// 需要管理员角色。
    /// </summary>
    /// <returns>部署健康检查结果。</returns>
    [HttpGet("detail")]
    [Authorize(Roles = "admin")]
    public virtual async Task<DeploymentHealthDto> GetDetailAsync()
    {
        return await _deploymentHealthCheckService.GetHealthAsync();
    }
}
