using System;
using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

public interface IWechatBindingTicketStore
{
    Task<string> CreateAsync(WechatIdentity identity, TimeSpan lifetime);

    Task<WechatIdentity?> GetAsync(string ticket);

    Task RemoveAsync(string ticket);
}
