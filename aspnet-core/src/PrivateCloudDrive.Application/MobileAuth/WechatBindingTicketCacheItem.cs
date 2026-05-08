using System;

namespace PrivateCloudDrive.MobileAuth;

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
