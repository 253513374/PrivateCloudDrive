using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.AdminIdentity;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.AdminIdentity;

/// <summary>
/// 管理员用户管理 HTTP API 控制器，仅 admin 角色可用。
/// </summary>
[Route("api/admin/identity/users")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class AdminIdentityUserController : PrivateCloudDriveController
{
    private readonly IAdminIdentityUserAppService _adminIdentityUserAppService;

    public AdminIdentityUserController(IAdminIdentityUserAppService adminIdentityUserAppService)
    {
        _adminIdentityUserAppService = adminIdentityUserAppService;
    }

    /// <summary>
    /// 获取用户分页列表。
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<AdminIdentityUserDto>> GetListAsync([FromQuery] PagedAndSortedResultRequestDto input)
    {
        return _adminIdentityUserAppService.GetListAsync(input);
    }

    /// <summary>
    /// 创建新用户。
    /// </summary>
    [HttpPost]
    public virtual Task<AdminIdentityUserDto> CreateAsync([FromBody] AdminCreateUserInput input)
    {
        return _adminIdentityUserAppService.CreateAsync(input);
    }

    /// <summary>
    /// 禁用一个用户。
    /// </summary>
    [HttpPost("{userId}/disable")]
    public virtual async Task<IActionResult> DisableAsync(Guid userId)
    {
        await _adminIdentityUserAppService.DisableAsync(userId);
        return Ok();
    }

    /// <summary>
    /// 启用一个用户。
    /// </summary>
    [HttpPost("{userId}/enable")]
    public virtual async Task<IActionResult> EnableAsync(Guid userId)
    {
        await _adminIdentityUserAppService.EnableAsync(userId);
        return Ok();
    }

    /// <summary>
    /// 重置用户密码。
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public virtual async Task<IActionResult> ResetPasswordAsync(Guid userId, [FromBody] AdminResetPasswordInput input)
    {
        await _adminIdentityUserAppService.ResetPasswordAsync(userId, input);
        return Ok();
    }

    /// <summary>
    /// 设置用户存储容量配额。
    /// </summary>
    [HttpPost("{userId}/set-quota")]
    public virtual async Task<IActionResult> SetQuotaAsync(Guid userId, [FromBody] AdminSetQuotaInput input)
    {
        await _adminIdentityUserAppService.SetQuotaAsync(userId, input);
        return Ok();
    }
}
