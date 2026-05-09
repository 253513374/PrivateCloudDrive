namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证MobileAuthAuditLogActions，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public static class MobileAuthAuditLogActions
{
    public const string PasswordLogin = "PasswordLogin";
    public const string RefreshToken = "RefreshToken";
    public const string Logout = "Logout";
    public const string WeChatLogin = "WeChatLogin";
    public const string WeChatBind = "WeChatBind";
    public const string WeChatUnbind = "WeChatUnbind";
    public const string ExternalLogin = "ExternalLogin";
    public const string ExternalBind = "ExternalBind";
    public const string ExternalUnbind = "ExternalUnbind";
}
