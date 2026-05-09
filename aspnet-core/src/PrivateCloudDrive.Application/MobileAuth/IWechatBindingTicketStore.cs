using System;
using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证IWechatBindingTicketStore，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public interface IWechatBindingTicketStore
{
    Task<string> CreateAsync(WechatIdentity identity, TimeSpan lifetime);

    Task<WechatIdentity?> GetAsync(string ticket);

    Task RemoveAsync(string ticket);
}
