using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// MAUI 客户端认证服务，统一封装密码登录、浏览器授权登录、微信/第三方扩展 grant 和 Token 存取。
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 判断本地是否存在可用访问令牌。
    /// </summary>
    Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用用户名和密码登录 OpenIddict password grant。
    /// </summary>
    Task SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用微信授权码调用后端微信扩展 grant。
    /// </summary>
    Task<WechatSignInResult> SignInWithWechatCodeAsync(
        string code,
        string? state,
        string? platform,
        string? deviceIdHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 调起系统浏览器完成 Google/GitHub 授权，并校验 state 与授权码。
    /// </summary>
    Task<ExternalAuthorizationResult> AuthorizeExternalAsync(
        ExternalLoginProviderSettings provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用第三方授权码调用后端通用第三方登录扩展 grant。
    /// </summary>
    Task<ExternalSignInResult> SignInWithExternalCodeAsync(
        string provider,
        string code,
        string? state,
        string redirectUri,
        string? codeVerifier,
        string? deviceIdHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理本地令牌并记录登出审计。
    /// </summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可用 access token；必要时尝试刷新。
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
