using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 使用分布式缓存保存第三方登录短期绑定票据。
/// 票据值为随机 Guid，不包含 Provider 身份信息，实际身份数据存放在缓存项中并随过期时间自动清理。
/// </summary>
[ExposeServices(
    typeof(IExternalBindingTicketStore),
    typeof(DistributedCacheExternalBindingTicketStore))]
public class DistributedCacheExternalBindingTicketStore :
    IExternalBindingTicketStore,
    ITransientDependency
{
    private readonly IDistributedCache<ExternalBindingTicketCacheItem, string> _cache;
    private readonly IGuidGenerator _guidGenerator;

    /// <summary>
    /// 初始化 <see cref="DistributedCacheExternalBindingTicketStore"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DistributedCacheExternalBindingTicketStore(
        IDistributedCache<ExternalBindingTicketCacheItem, string> cache,
        IGuidGenerator guidGenerator)
    {
        _cache = cache;
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// 创建绑定票据并设置绝对过期时间，降低票据被重复使用或长期滞留的风险。
    /// </summary>
    public virtual async Task<string> CreateAsync(ExternalIdentity identity, TimeSpan lifetime)
    {
        var ticket = _guidGenerator.Create().ToString("N");
        var now = DateTime.UtcNow;
        var item = new ExternalBindingTicketCacheItem
        {
            Provider = identity.Provider,
            ProviderUserId = identity.ProviderUserId,
            Email = identity.Email,
            DisplayName = identity.DisplayName,
            AvatarUrl = identity.AvatarUrl,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime)
        };

        await _cache.SetAsync(
            ticket,
            item,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            });

        return ticket;
    }

    /// <summary>
    /// 读取票据对应的第三方身份；过期票据按不存在处理。
    /// </summary>
    public virtual async Task<ExternalIdentity?> GetAsync(string ticket)
    {
        var item = await _cache.GetAsync(ticket);
        if (item == null || item.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return new ExternalIdentity
        {
            Provider = item.Provider,
            ProviderUserId = item.ProviderUserId,
            Email = item.Email,
            DisplayName = item.DisplayName,
            AvatarUrl = item.AvatarUrl
        };
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    public virtual Task RemoveAsync(string ticket)
    {
        return _cache.RemoveAsync(ticket);
    }
}
