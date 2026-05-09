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

/// <summary>
/// 基于 ABP 分布式缓存的第三方认证限流器。
/// 缓存键使用租户、操作和主体的 SHA-256 哈希，避免把用户名、设备标识等原始信息写入缓存键。
/// </summary>
[ExposeServices(
    typeof(IExternalAuthRateLimiter),
    typeof(DistributedCacheExternalAuthRateLimiter))]
public class DistributedCacheExternalAuthRateLimiter :
    IExternalAuthRateLimiter,
    ITransientDependency
{
    private readonly IDistributedCache<ExternalAuthRateLimitCacheItem, string> _cache;
    private readonly ExternalLoginOptions _options;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 初始化 <see cref="DistributedCacheExternalAuthRateLimiter"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DistributedCacheExternalAuthRateLimiter(
        IDistributedCache<ExternalAuthRateLimitCacheItem, string> cache,
        IOptions<ExternalLoginOptions> options,
        ICurrentTenant currentTenant)
    {
        _cache = cache;
        _options = options.Value;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 在固定时间窗口内累计失败/敏感操作次数，超过阈值时阻止继续尝试。
    /// </summary>
    public virtual async Task CheckAsync(string operation, string subject)
    {
        var maxAttempts = Math.Max(1, _options.RateLimitMaxAttempts);
        var window = TimeSpan.FromSeconds(Math.Max(1, _options.RateLimitWindowSeconds));
        var now = DateTime.UtcNow;
        var cacheKey = BuildCacheKey(operation, subject);

        var item = await _cache.GetAsync(cacheKey);
        if (item == null || item.ExpiresAt <= now)
        {
            item = new ExternalAuthRateLimitCacheItem
            {
                Count = 0,
                WindowStartedAt = now,
                ExpiresAt = now.Add(window)
            };
        }

        if (item.Count >= maxAttempts)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginRateLimited)
                .WithData("error", ExternalLoginConsts.RateLimitedError);
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
