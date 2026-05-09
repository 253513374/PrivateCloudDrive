namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示移动认证WechatLoginSettings，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public sealed record WechatLoginSettings(
    bool IsEnabled,
    string? AppId,
    string Scope,
    string CallbackScheme,
    string? AndroidPackageName,
    string? IosBundleId,
    string? IosUrlScheme);
