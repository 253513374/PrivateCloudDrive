using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 部署健康检查应用服务契约，为运维人员提供无认证的部署后系统就绪确认。
/// </summary>
public interface IDeploymentHealthCheckService : IApplicationService
{
    /// <summary>
    /// 获取部署健康检查完整结果，包含所有组件的 Pass/Warn/Fail 状态和修复建议。
    /// </summary>
    Task<DeploymentHealthDto> GetHealthAsync();
}
