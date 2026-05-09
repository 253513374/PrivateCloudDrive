namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示WechatLogin请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class WechatLoginInput
{
    public string Code { get; init; } = string.Empty;

    public string? State { get; init; }

    public string? Platform { get; init; }

    public string? DeviceIdHash { get; init; }

    public string? ClientId { get; init; }
}
