using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

public interface IWechatIdentityService
{
    Task<WechatIdentity> ExchangeAsync(
        string code,
        string? platform = null,
        CancellationToken cancellationToken = default);
}
