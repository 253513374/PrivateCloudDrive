using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 文件中心容量 HTTP API 控制器。
/// </summary>
[Route("api/file-center/storage")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterStorageController : PrivateCloudDriveController
{
    private readonly IFileCenterStorageAppService _storageAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterStorageController"/> 的新实例。
    /// </summary>
    public FileCenterStorageController(IFileCenterStorageAppService storageAppService)
    {
        _storageAppService = storageAppService;
    }

    /// <summary>
    /// 获取当前用户容量使用摘要。
    /// </summary>
    [HttpGet("usage")]
    public virtual Task<StorageUsageDto> GetUsageAsync()
    {
        return _storageAppService.GetUsageAsync();
    }
}
