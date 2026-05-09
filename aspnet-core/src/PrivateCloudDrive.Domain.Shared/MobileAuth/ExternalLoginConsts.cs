namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// Google/GitHub 等通用第三方登录使用的 Provider 名称、错误码和 grant 常量。
/// 错误码会透传到移动端和审计日志，应保持稳定。
/// </summary>
public static class ExternalLoginConsts
{
    public const string GrantType = "urn:privateclouddrive:external";
    public const string GoogleProviderName = "Google";
    public const string GitHubProviderName = "GitHub";
    public const string BindingRequiredError = "external_binding_required";
    public const string DisabledError = "external_login_disabled";
    public const string CodeExchangeFailedError = "external_code_exchange_failed";
    public const string AlreadyBoundError = "external_already_bound";
    public const string BindingTicketNotFoundError = "external_binding_ticket_not_found";
    public const string BindingNotFoundError = "external_binding_not_found";
    public const string UnbindNotAllowedError = "external_unbind_not_allowed";
    public const string RateLimitedError = "external_rate_limited";
    public const string ProviderUnsupportedError = "external_provider_unsupported";
    public const string AutoProvisionFailedError = "external_auto_provision_failed";

    /// <summary>
    /// 将客户端传入的 Provider 名称规范化为系统内部常量，避免大小写和空白导致重复绑定。
    /// </summary>
    public static string? NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "google" => GoogleProviderName,
            "github" => GitHubProviderName,
            _ => null
        };
    }
}
