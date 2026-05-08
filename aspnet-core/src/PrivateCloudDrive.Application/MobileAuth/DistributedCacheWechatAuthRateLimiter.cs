using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.MobileAuth;

[ExposeServices(
    typeof(IWechatAuthRateLimiter),
    typeof(DistributedCacheWechatAuthRateLimiter))]
public class DistributedCacheWechatAuthRateLimiter :
    IWechatAuthRateLimiter,
    ITransientDependency
{
    private readonly IDistributedCache<WechatAuthRateLimitCacheItem, string> _cache;
    private readonly WechatLoginOptions _options;
    private readonly ICurrentTenant _currentTenant;

    public DistributedCacheWechatAuthRateLimiter(
        IDistributedCache<WechatAuthRateLimitCacheItem, string> cache,
        IOptions<WechatLoginOptions> options,
        ICurrentTenant currentTenant)
    {
        _cache = cache;
        _options = options.Value;
        _currentTenant = currentTenant;
    }

    public virtual async Task CheckAsync(string operation, string subject)
    {
        var maxAttempts = Math.Max(1, _options.RateLimitMaxAttempts);
        var window = TimeSpan.FromSeconds(Math.Max(1, _options.RateLimitWindowSeconds));
        var now = DateTime.UtcNow;
        var cacheKey = BuildCacheKey(operation, subject);

        var item = await _cache.GetAsync(cacheKey);
        if (item == null || item.ExpiresAt <= now)
        {
            item = new WechatAuthRateLimitCacheItem
            {
                Count = 0,
                WindowStartedAt = now,
                ExpiresAt = now.Add(window)
            };
        }

        if (item.Count >= maxAttempts)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatRateLimited)
                .WithData("error", WechatLoginConsts.RateLimitedError);
        }

        item.Count++;
        await _cache.SetAsync(
            cacheKey,
            item,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = item.ExpiresAt - now
            });
    }

    private string BuildCacheKey(string operation, string subject)
    {
        var tenant = _currentTenant.Id?.ToString("N") ?? "host";
        var rawKey = $"{tenant}:{operation}:{subject}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
