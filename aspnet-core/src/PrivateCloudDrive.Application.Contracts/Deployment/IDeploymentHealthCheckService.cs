using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 部署健康检查应用服务契约，为运维人员提供无认证的部署后系统就绪确认。
/// 所有输出均不包含密码、token、OAuth code、client secret 或完整私有 URL。
/// </summary>
public interface IDeploymentHealthCheckService : IApplicationService
{
    /// <summary>
    /// 获取部署健康检查完整结果，包含所有组件的 Pass/Warn/Fail 状态和修复建议。
    /// 仅限管理员访问。
    /// </summary>
    Task<DeploymentHealthDto> GetHealthAsync();

    /// <summary>
    /// 获取进程存活检查结果，不依赖任何外部服务。
    /// 适用于负载均衡器和编排平台的存活探针。
    /// </summary>
    Task<DeploymentLiveDto> GetLiveAsync();

    /// <summary>
    /// 获取部署就绪检查结果，仅返回低敏依赖状态。
    /// 适用于编排平台的就绪探针，不含修复建议或敏感详情。
    /// </summary>
    Task<DeploymentReadyDto> GetReadyAsync();
}
