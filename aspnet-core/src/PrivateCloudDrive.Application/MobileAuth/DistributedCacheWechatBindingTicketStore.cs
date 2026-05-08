using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace PrivateCloudDrive.MobileAuth;

[ExposeServices(
    typeof(IWechatBindingTicketStore),
    typeof(DistributedCacheWechatBindingTicketStore))]
public class DistributedCacheWechatBindingTicketStore :
    IWechatBindingTicketStore,
    ITransientDependency
{
    private readonly IDistributedCache<WechatBindingTicketCacheItem, string> _cache;
    private readonly IGuidGenerator _guidGenerator;

    public DistributedCacheWechatBindingTicketStore(
        IDistributedCache<WechatBindingTicketCacheItem, string> cache,
        IGuidGenerator guidGenerator)
    {
        _cache = cache;
        _guidGenerator = guidGenerator;
    }

    public virtual async Task<string> CreateAsync(WechatIdentity identity, TimeSpan lifetime)
    {
        var ticket = _guidGenerator.Create().ToString("N");
        var now = DateTime.UtcNow;
        var item = new WechatBindingTicketCacheItem
        {
            AppId = identity.AppId,
            OpenId = identity.OpenId,
            UnionId = identity.UnionId,
            NickName = identity.NickName,
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

    public virtual async Task<WechatIdentity?> GetAsync(string ticket)
    {
        var item = await _cache.GetAsync(ticket);
        if (item == null || item.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return new WechatIdentity
        {
            AppId = item.AppId,
            OpenId = item.OpenId,
            UnionId = item.UnionId,
            NickName = item.NickName,
            AvatarUrl = item.AvatarUrl
        };
    }

    public virtual Task RemoveAsync(string ticket)
    {
        return _cache.RemoveAsync(ticket);
    }
}
