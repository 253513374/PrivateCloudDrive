namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 管理员用户数据，映射后端 AdminIdentityUser API 的返回结构。
/// 包含用户名、邮箱、角色列表和账户启用状态。
/// </summary>
public sealed record AdminUserDto(
    Guid Id,
    string UserName,
    string Email,
    string[] Roles,
    bool IsActive,
    DateTime CreationTime);
