namespace PrivateCloudDrive.MobileAuth;

public class WechatLoginSettingsDto
{
    public bool IsEnabled { get; set; }

    public string? AppId { get; set; }

    public string Scope { get; set; } = "snsapi_userinfo";

    public string CallbackScheme { get; set; } = "privateclouddrive";

    public string? AndroidPackageName { get; set; }

    public string? IosBundleId { get; set; }

    public string? IosUrlScheme { get; set; }
}
