namespace PrivateCloudDrive.App.Services;

public static class AppSettings
{
    public static string ApiBaseUrl
    {
        get
        {
#if ANDROID
            return "http://10.0.2.2:8080";
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
