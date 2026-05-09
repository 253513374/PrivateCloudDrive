namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示移动认证WechatBinding，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public sealed record WechatBinding(
    Guid Id,
    Guid? TenantId,
    Guid UserId,
    string AppId,
    string? NickName,
    string? AvatarUrl,
    bool IsEnabled,
    DateTime? LastLoginTime,
    DateTime CreationTime);
