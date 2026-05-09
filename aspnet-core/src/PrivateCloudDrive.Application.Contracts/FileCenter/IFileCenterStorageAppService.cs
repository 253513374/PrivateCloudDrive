using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心存储容量应用服务契约，用于客户端展示当前用户容量使用情况。
/// </summary>
public interface IFileCenterStorageAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户的存储容量摘要。
    /// </summary>
    Task<StorageUsageDto> GetUsageAsync();
}
