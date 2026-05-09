namespace PrivateCloudDrive.App.Models;

/// <summary>
/// MAUI 设置页展示的第三方账号绑定信息。
/// </summary>
public sealed record ExternalBinding(
    Guid Id,
    Guid? TenantId,
    Guid UserId,
    string Provider,
    string? Email,
    string? DisplayName,
    string? AvatarUrl,
    bool IsEnabled,
    DateTime? LastLoginTime,
    DateTime CreationTime);
