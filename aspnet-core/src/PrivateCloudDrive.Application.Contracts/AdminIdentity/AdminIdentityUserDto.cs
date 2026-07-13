using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.AdminIdentity;

/// <summary>
/// 管理员用户管理 DTO：展示用户基本信息、启用状态和配额。
/// </summary>
public class AdminIdentityUserDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public long? StorageQuotaBytes { get; set; }

    public long? StorageUsedBytes { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastLoginTime { get; set; }
}

/// <summary>
/// 管理员创建用户输入。
/// </summary>
public class AdminCreateUserInput
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 存储容量配额（字节）。不传或 0 表示使用默认配额。
    /// </summary>
    public long? StorageQuotaBytes { get; set; }

    /// <summary>
    /// 初始角色列表，例如 ["admin"] 或 ["user"]。不传默认为普通用户。
    /// </summary>
    public string[]? RoleNames { get; set; }
}

/// <summary>
/// 管理员重置密码输入。
/// </summary>
public class AdminResetPasswordInput
{
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 管理员设置容量配额输入。
/// </summary>
public class AdminSetQuotaInput
{
    /// <summary>
    /// 容量配额（字节）。传 0 表示不限制。
    /// </summary>
    public long StorageQuotaBytes { get; set; }
}
