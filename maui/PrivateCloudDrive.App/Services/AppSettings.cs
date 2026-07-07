using Microsoft.Maui.Storage;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示AppSettings组件，封装对应业务场景的状态或行为。
/// </summary>
public static class AppSettings
{
    private const string CustomApiBaseUrlKey = "settings.apiBaseUrl";
    private const string AndroidEmulatorApiBaseUrl = "http://10.0.2.2:8081";
    private const string AndroidDeviceApiBaseUrl = "http://192.168.1.94:8081";
    private const string DevelopmentWindowsApiBaseUrl = "http://localhost:8081";
    private const string ProductionApiBaseUrl = "https://privateclouddrive.example.com";

    public static string ApiBaseUrl
    {
        get
        {
            var customUrl = Preferences.Default.Get(CustomApiBaseUrlKey, string.Empty);
            return string.IsNullOrWhiteSpace(customUrl)
                ? DefaultApiBaseUrl
                : NormalizeApiBaseUrl(customUrl);
        }
    }

    public static string DefaultApiBaseUrl
    {
        get
        {
#if DEBUG
#if ANDROID
            return Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual
                ? AndroidEmulatorApiBaseUrl
                : AndroidDeviceApiBaseUrl;
#else
            return DevelopmentWindowsApiBaseUrl;
#endif
#else
            return ProductionApiBaseUrl;
#endif
        }
    }

    public static bool HasCustomApiBaseUrl =>
        !string.IsNullOrWhiteSpace(Preferences.Default.Get(CustomApiBaseUrlKey, string.Empty));

    public static void SetApiBaseUrl(string value)
    {
        Preferences.Default.Set(CustomApiBaseUrlKey, NormalizeApiBaseUrl(value));
    }

    public static void ResetApiBaseUrl()
    {
        Preferences.Default.Remove(CustomApiBaseUrlKey);
    }

    public const string OAuthClientId = "PrivateCloudDrive_App";

    public const string OAuthRedirectUri = "privateclouddrive://callback";

    public const string WindowsOAuthRedirectUri = "http://127.0.0.1:49173/callback";

    public const string OAuthScopes = "openid profile email roles offline_access PrivateCloudDrive";

    private static string NormalizeApiBaseUrl(string value)
    {
        var normalized = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("请输入有效的 http 或 https 后端地址。", nameof(value));
        }

        return normalized;
    }
}
