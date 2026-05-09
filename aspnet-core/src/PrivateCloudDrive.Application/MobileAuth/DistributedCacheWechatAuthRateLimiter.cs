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
/// 表示移动认证DistributedCacheWechatAuthRateLimiter，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
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

    /// <summary>
    /// 初始化 <see cref="DistributedCacheWechatAuthRateLimiter"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DistributedCacheWechatAuthRateLimiter(
        IDistributedCache<WechatAuthRateLimitCacheItem, string> cache,
        IOptions<WechatLoginOptions> options,
        ICurrentTenant currentTenant)
    {
        _cache = cache;
        _options = options.Value;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 检查当前操作是否满足安全限制或业务前置条件。
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
