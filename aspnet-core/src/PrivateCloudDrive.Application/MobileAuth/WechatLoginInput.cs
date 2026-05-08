namespace PrivateCloudDrive.MobileAuth;

public class WechatLoginInput
{
    public string Code { get; init; } = string.Empty;

    public string? State { get; init; }

    public string? Platform { get; init; }

    public string? DeviceIdHash { get; init; }

    public string? ClientId { get; init; }
}
