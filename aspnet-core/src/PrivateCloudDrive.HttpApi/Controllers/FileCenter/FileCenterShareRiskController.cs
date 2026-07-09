using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 分享风险提示 HTTP API 控制器。
/// 聚合分享风险指标并返回可读文案，不暴露敏感数据。
/// 普通用户只能查看自己的风险；管理员可以查询任意用户。
/// </summary>
[Route("api/file-center/shares/risk")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Share)]
public class FileCenterShareRiskController : PrivateCloudDriveController
{
    private readonly IFileCenterShareRiskAppService _shareRiskAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterShareRiskController"/> 的新实例。
    /// </summary>
    public FileCenterShareRiskController(IFileCenterShareRiskAppService shareRiskAppService)
    {
        _shareRiskAppService = shareRiskAppService;
    }

    /// <summary>
    /// 获取当前用户的分享风险提示。
    /// </summary>
    [HttpGet]
    public virtual Task<ShareRiskDto> GetMyRiskAsync()
    {
        return _shareRiskAppService.GetMyRiskAsync();
    }

    /// <summary>
    /// 管理员查询指定用户的分享风险。
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<ShareRiskDto> GetUserRiskAsync(Guid userId)
    {
        return _shareRiskAppService.GetUserRiskAsync(userId);
    }
}
