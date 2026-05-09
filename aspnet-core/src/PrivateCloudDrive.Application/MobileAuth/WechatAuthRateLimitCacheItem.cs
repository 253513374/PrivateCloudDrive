using System;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证WechatAuthRateLimitCacheItem，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class WechatAuthRateLimitCacheItem
{
    public int Count { get; set; }

    public DateTime WindowStartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
