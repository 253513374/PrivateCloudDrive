namespace PrivateCloudDrive.App.Services;

public sealed record WechatPlatformAuthResult(
    bool Succeeded,
    string? Code,
    string? State,
    string? Platform,
    string? ErrorMessage)
{
    public static WechatPlatformAuthResult Failure(string message)
    {
        return new WechatPlatformAuthResult(false, null, null, null, message);
    }
}
