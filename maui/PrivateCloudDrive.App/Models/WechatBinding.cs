namespace PrivateCloudDrive.App.Models;

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
