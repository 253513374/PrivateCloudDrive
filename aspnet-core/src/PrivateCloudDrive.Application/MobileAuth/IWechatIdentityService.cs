using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 提供IWechatIdentity服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public interface IWechatIdentityService
{
    Task<WechatIdentity> ExchangeAsync(
        string code,
        string? platform = null,
        CancellationToken cancellationToken = default);
}
