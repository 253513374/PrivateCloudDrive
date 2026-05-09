using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证DistributedCacheWechatBindingTicketStore，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
[ExposeServices(
    typeof(IWechatBindingTicketStore),
    typeof(DistributedCacheWechatBindingTicketStore))]
public class DistributedCacheWechatBindingTicketStore :
    IWechatBindingTicketStore,
    ITransientDependency
{
    private readonly IDistributedCache<WechatBindingTicketCacheItem, string> _cache;
    private readonly IGuidGenerator _guidGenerator;

    /// <summary>
    /// 初始化 <see cref="DistributedCacheWechatBindingTicketStore"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DistributedCacheWechatBindingTicketStore(
        IDistributedCache<WechatBindingTicketCacheItem, string> cache,
        IGuidGenerator guidGenerator)
    {
        _cache = cache;
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
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

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
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

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    public virtual Task RemoveAsync(string ticket)
    {
        return _cache.RemoveAsync(ticket);
    }
}
