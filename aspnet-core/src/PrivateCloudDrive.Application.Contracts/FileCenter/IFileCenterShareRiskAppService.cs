using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 分享风险提示应用服务契约。
/// 聚合分享风险指标并返回可读文案，不暴露敏感数据。
/// </summary>
public interface IFileCenterShareRiskAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户的分享风险提示。
    /// </summary>
    Task<ShareRiskDto> GetMyRiskAsync();

    /// <summary>
    /// 管理员查询指定用户的分享风险。
    /// </summary>
    Task<ShareRiskDto> GetUserRiskAsync(Guid userId);
}
