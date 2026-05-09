using System;
using System.Collections.Generic;
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
/// 表示移动认证DistributedCachePasswordLoginRateLimiter，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
[ExposeServices(
    typeof(IPasswordLoginRateLimiter),
    typeof(DistributedCachePasswordLoginRateLimiter))]
public class DistributedCachePasswordLoginRateLimiter :
    IPasswordLoginRateLimiter,
    ITransientDependency
{
    private readonly IDistributedCache<PasswordLoginRateLimitCacheItem, string> _cache;
    private readonly MobileAuthLoginOptions _options;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 初始化 <see cref="DistributedCachePasswordLoginRateLimiter"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DistributedCachePasswordLoginRateLimiter(
        IDistributedCache<PasswordLoginRateLimitCacheItem, string> cache,
        IOptions<MobileAuthLoginOptions> options,
        ICurrentTenant currentTenant)
    {
        _cache = cache;
        _options = options.Value;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 检查当前操作是否满足安全限制或业务前置条件。
    /// </summary>
    public virtual async Task CheckAsync(string? userName, string? ipAddress)
    {
        if (!_options.EnablePasswordLoginRateLimit)
        {
            return;
        }

        var maxAttempts = Math.Max(1, _options.MaxFailedAttempts);

        foreach (var cacheKey in BuildCacheKeys(userName, ipAddress))
        {
            var item = await _cache.GetAsync(cacheKey);
            if (item != null && item.ExpiresAt > DateTime.UtcNow && item.Count >= maxAttempts)
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.PasswordLoginRateLimited)
                    .WithData("error", PasswordLoginConsts.RateLimitedError);
            }
        }
    }

    /// <summary>
    /// 记录业务事件或安全事件，便于后续审计、追踪和风险分析。
    /// </summary>
    public virtual async Task RecordFailureAsync(string? userName, string? ipAddress)
    {
        if (!_options.EnablePasswordLoginRateLimit)
        {
            return;
        }

        var window = TimeSpan.FromMinutes(Math.Max(1, _options.WindowMinutes));
        var now = DateTime.UtcNow;

        foreach (var cacheKey in BuildCacheKeys(userName, ipAddress))
        {
            var item = await _cache.GetAsync(cacheKey);
            if (item == null || item.ExpiresAt <= now)
            {
                item = new PasswordLoginRateLimitCacheItem
                {
                    Count = 0,
                    WindowStartedAt = now,
                    ExpiresAt = now.Add(window)
                };
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
    }

    /// <summary>
    /// 重置指定对象的临时安全状态或缓存状态。
    /// </summary>
    public virtual async Task ResetUserAsync(string? userName)
    {
        if (!_options.EnablePasswordLoginRateLimit || string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        await _cache.RemoveAsync(BuildCacheKey("user", Normalize(userName)));
    }

    private IEnumerable<string> BuildCacheKeys(string? userName, string? ipAddress)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            yield return BuildCacheKey("user", Normalize(userName));
        }

        yield return BuildCacheKey("ip", Normalize(string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress));
    }

    private string BuildCacheKey(string dimension, string subject)
    {
        var tenant = _currentTenant.Id?.ToString("N") ?? "host";
        var rawKey = $"{tenant}:password-login:{dimension}:{subject}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
