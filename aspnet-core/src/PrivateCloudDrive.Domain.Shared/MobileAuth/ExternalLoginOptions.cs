namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 通用第三方登录配置。
/// Secret 只允许从后端配置、环境变量或密钥系统读取，不能下发到 MAUI 客户端。
/// </summary>
public class ExternalLoginOptions
{
    public int BindingTicketLifetimeMinutes { get; set; } = 5;

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int RateLimitWindowSeconds { get; set; } = 300;

    public int RateLimitMaxAttempts { get; set; } = 60;

    public ExternalLoginProviderOptions Google { get; set; } = ExternalLoginProviderOptions.CreateGoogle();

    public ExternalLoginProviderOptions GitHub { get; set; } = ExternalLoginProviderOptions.CreateGitHub();

    /// <summary>
    /// 根据 Provider 名称返回对应配置；未知 Provider 返回 null。
    /// </summary>
    public ExternalLoginProviderOptions? GetProvider(string? provider)
    {
        return ExternalLoginConsts.NormalizeProvider(provider) switch
        {
            ExternalLoginConsts.GoogleProviderName => Google,
            ExternalLoginConsts.GitHubProviderName => GitHub,
            _ => null
        };
    }
}

/// <summary>
/// 单个第三方登录 Provider 的 OAuth/OIDC 端点和客户端配置。
/// </summary>
public class ExternalLoginProviderOptions
{
    public bool Enabled { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string AuthorizationEndpoint { get; set; } = string.Empty;

    public string TokenEndpoint { get; set; } = string.Empty;

    public string UserInfoEndpoint { get; set; } = string.Empty;

    public string EmailsEndpoint { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = "privateclouddrive://callback";

    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// 判断 Provider 是否已具备发起授权和后端换取身份的最低配置。
    /// </summary>
    public bool IsUsable(bool requireClientSecret)
    {
        return Enabled &&
               !string.IsNullOrWhiteSpace(ClientId) &&
               (!requireClientSecret || !string.IsNullOrWhiteSpace(ClientSecret)) &&
               !string.IsNullOrWhiteSpace(AuthorizationEndpoint) &&
               !string.IsNullOrWhiteSpace(TokenEndpoint) &&
               !string.IsNullOrWhiteSpace(UserInfoEndpoint) &&
               !string.IsNullOrWhiteSpace(RedirectUri);
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public static ExternalLoginProviderOptions CreateGoogle()
    {
        return new ExternalLoginProviderOptions
        {
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo",
            Scope = "openid profile email",
            RedirectUri = "privateclouddrive://callback",
            UsePkce = true
        };
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public static ExternalLoginProviderOptions CreateGitHub()
    {
        return new ExternalLoginProviderOptions
        {
            AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
            TokenEndpoint = "https://github.com/login/oauth/access_token",
            UserInfoEndpoint = "https://api.github.com/user",
            EmailsEndpoint = "https://api.github.com/user/emails",
            Scope = "read:user user:email",
            RedirectUri = "privateclouddrive://callback",
            UsePkce = true
        };
    }
}
