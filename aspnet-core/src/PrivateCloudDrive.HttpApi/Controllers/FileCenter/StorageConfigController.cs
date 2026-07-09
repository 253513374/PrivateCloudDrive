using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 管理员存储配置 HTTP API 控制器（只读）。
/// </summary>
[Route("api/admin/storage-config")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class StorageConfigController : PrivateCloudDriveController
{
    private readonly IStorageConfigAppService _storageConfigAppService;

    /// <summary>
    /// 初始化 <see cref="StorageConfigController"/> 的新实例。
    /// </summary>
    public StorageConfigController(IStorageConfigAppService storageConfigAppService)
    {
        _storageConfigAppService = storageConfigAppService;
    }

    /// <summary>
    /// 获取存储配置信息（只读）。
    /// </summary>
    [HttpGet]
    public virtual Task<StorageConfigDto> GetAsync()
    {
        return _storageConfigAppService.GetAsync();
    }
}
