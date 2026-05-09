namespace PrivateCloudDrive.App.Models;

/// <summary>
/// MAUI 客户端可见的第三方登录 Provider 配置。
/// 不包含 ClientSecret 或任何 Provider Token。
/// </summary>
public sealed record ExternalLoginProviderSettings(
    string Provider,
    string DisplayName,
    bool IsEnabled,
    string? ClientId,
    string AuthorizationEndpoint,
    string Scope,
    string RedirectUri,
    bool UsePkce);
