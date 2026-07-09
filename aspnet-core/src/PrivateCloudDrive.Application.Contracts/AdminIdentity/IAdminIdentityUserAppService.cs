using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.AdminIdentity;

/// <summary>
/// 管理员用户管理应用服务契约。
/// </summary>
public interface IAdminIdentityUserAppService : IApplicationService
{
    /// <summary>
    /// 获取用户分页列表，管理员可查看所有用户。
    /// </summary>
    Task<PagedResultDto<AdminIdentityUserDto>> GetListAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// 创建新用户。
    /// </summary>
    Task<AdminIdentityUserDto> CreateAsync(AdminCreateUserInput input);

    /// <summary>
    /// 禁用一个用户。
    /// </summary>
    Task DisableAsync(Guid userId);

    /// <summary>
    /// 启用一个用户。
    /// </summary>
    Task EnableAsync(Guid userId);

    /// <summary>
    /// 重置用户密码。
    /// </summary>
    Task ResetPasswordAsync(Guid userId, AdminResetPasswordInput input);

    /// <summary>
    /// 设置用户存储容量配额。
    /// </summary>
    Task SetQuotaAsync(Guid userId, AdminSetQuotaInput input);
}
