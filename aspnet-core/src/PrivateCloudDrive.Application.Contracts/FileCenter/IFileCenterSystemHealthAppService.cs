using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心系统健康应用服务契约，为移动端设置页提供安全的运行状态摘要。
/// </summary>
public interface IFileCenterSystemHealthAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户可见的文件中心系统健康摘要。
    /// </summary>
    Task<FileCenterSystemHealthDto> GetSummaryAsync();
}
