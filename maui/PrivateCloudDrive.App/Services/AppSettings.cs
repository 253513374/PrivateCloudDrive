namespace PrivateCloudDrive.App.Services;

public static class AppSettings
{
    public const string ApiBaseUrl = "https://localhost:44343";

    public const string OAuthClientId = "PrivateCloudDrive_App";

    public const string OAuthRedirectUri = "privateclouddrive://callback";

    public const string OAuthScopes = "openid profile email roles offline_access PrivateCloudDrive";
}
