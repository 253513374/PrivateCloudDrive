using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 封装与第三方 Provider 的授权码换取 Token、获取用户资料流程。
/// </summary>
public interface IExternalIdentityService
{
    /// <summary>
    /// 使用授权码换取第三方用户身份；实现层必须避免泄露 access token 和 provider secret。
    /// </summary>
    Task<ExternalIdentity> ExchangeAsync(
        string provider,
        string code,
        string redirectUri,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default);
}
