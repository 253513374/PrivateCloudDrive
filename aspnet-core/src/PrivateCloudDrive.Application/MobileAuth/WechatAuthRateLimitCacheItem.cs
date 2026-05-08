using System;

namespace PrivateCloudDrive.MobileAuth;

public class WechatAuthRateLimitCacheItem
{
    public int Count { get; set; }

    public DateTime WindowStartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
