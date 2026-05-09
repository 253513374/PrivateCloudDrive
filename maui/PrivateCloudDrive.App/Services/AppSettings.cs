namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示AppSettings组件，封装对应业务场景的状态或行为。
/// </summary>
public static class AppSettings
{
    private const string AndroidEmulatorApiBaseUrl = "http://10.0.2.2:8080";
    private const string AndroidDeviceApiBaseUrl = "http://192.168.1.94:8080";

    public static string ApiBaseUrl
    {
        get
        {
#if ANDROID
            return Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual
                ? AndroidEmulatorApiBaseUrl
                : AndroidDeviceApiBaseUrl;
#else
            return "http://localhost:8080";
#endif
        }
    }

    public const string OAuthClientId = "PrivateCloudDrive_App";

    public const string OAuthRedirectUri = "privateclouddrive://callback";

    public const string WindowsOAuthRedirectUri = "http://127.0.0.1:49173/callback";

    public const string OAuthScopes = "openid profile email roles offline_access PrivateCloudDrive";
}
