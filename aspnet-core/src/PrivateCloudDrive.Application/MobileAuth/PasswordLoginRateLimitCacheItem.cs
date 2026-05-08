using System;

namespace PrivateCloudDrive.MobileAuth;

public class PasswordLoginRateLimitCacheItem
{
    public int Count { get; set; }

    public DateTime WindowStartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
