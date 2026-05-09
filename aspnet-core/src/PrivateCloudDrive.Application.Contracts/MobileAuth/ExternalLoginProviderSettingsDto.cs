namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 第三方登录 Provider 的公开客户端配置。
/// 该 DTO 会返回给 MAUI App，禁止加入 ClientSecret、access token、refresh token 等敏感字段。
/// </summary>
public class ExternalLoginProviderSettingsDto
{
    public string Provider { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string? ClientId { get; set; }

    public string AuthorizationEndpoint { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public bool UsePkce { get; set; }
}
