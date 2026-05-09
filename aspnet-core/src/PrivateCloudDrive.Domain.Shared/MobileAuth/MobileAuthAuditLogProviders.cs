namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证MobileAuthAuditLogProviders，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public static class MobileAuthAuditLogProviders
{
    public const string Password = "Password";
    public const string RefreshToken = "RefreshToken";
    public const string WeChat = "WeChat";
    public const string Google = "Google";
    public const string GitHub = "GitHub";
}
