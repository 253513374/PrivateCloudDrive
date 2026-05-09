namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// OpenIddict 第三方登录扩展 grant 传入应用服务的参数。
/// </summary>
public class ExternalLoginInput
{
    public string Provider { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? State { get; init; }

    public string RedirectUri { get; init; } = string.Empty;

    public string? CodeVerifier { get; init; }

    public string? DeviceIdHash { get; init; }

    public string? ClientId { get; init; }
}
