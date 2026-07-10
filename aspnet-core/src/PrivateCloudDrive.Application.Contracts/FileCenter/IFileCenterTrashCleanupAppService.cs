using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 回收站清理建议应用服务契约。
/// 提供空间占用统计、保留天数和清理建议文案。
/// </summary>
public interface IFileCenterTrashCleanupAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户回收站的清理建议。
    /// </summary>
    Task<TrashCleanupAdviceDto> GetAdviceAsync();
}
