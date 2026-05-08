namespace PrivateCloudDrive.App.Models;

public sealed record WechatLoginSettings(
    bool IsEnabled,
    string? AppId,
    string Scope,
    string CallbackScheme,
    string? AndroidPackageName,
    string? IosBundleId,
    string? IosUrlScheme);
