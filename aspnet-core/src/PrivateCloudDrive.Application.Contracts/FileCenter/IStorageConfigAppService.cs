using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 存储配置只读应用服务契约。
/// </summary>
public interface IStorageConfigAppService : IApplicationService
{
    /// <summary>
    /// 获取存储配置信息（只读）。
    /// </summary>
    Task<StorageConfigDto> GetAsync();
}
