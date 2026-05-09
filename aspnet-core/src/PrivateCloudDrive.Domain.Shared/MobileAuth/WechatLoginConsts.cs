namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 定义WechatLogin相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class WechatLoginConsts
{
    public const string GrantType = "urn:privateclouddrive:wechat";
    public const string ProviderName = "WeChat";
    public const string BindingRequiredError = "wechat_binding_required";
    public const string DisabledError = "wechat_disabled";
    public const string CodeExchangeFailedError = "wechat_code_exchange_failed";
    public const string AlreadyBoundError = "wechat_already_bound";
    public const string BindingTicketNotFoundError = "wechat_binding_ticket_not_found";
    public const string BindingNotFoundError = "wechat_binding_not_found";
    public const string UnbindNotAllowedError = "wechat_unbind_not_allowed";
    public const string RateLimitedError = "wechat_rate_limited";
}
