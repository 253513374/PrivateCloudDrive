using System;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证ExternalBindingTicketCacheItem，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class ExternalBindingTicketCacheItem
{
    public string Provider { get; set; } = string.Empty;

    public string ProviderUserId { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
