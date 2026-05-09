using System;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证WechatBindingTicketCacheItem，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class WechatBindingTicketCacheItem
{
    public string AppId { get; set; } = string.Empty;

    public string OpenId { get; set; } = string.Empty;

    public string? UnionId { get; set; }

    public string? NickName { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
