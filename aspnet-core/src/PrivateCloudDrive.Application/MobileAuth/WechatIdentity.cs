namespace PrivateCloudDrive.MobileAuth;

public class WechatIdentity
{
    public string AppId { get; init; } = string.Empty;

    public string OpenId { get; init; } = string.Empty;

    public string? UnionId { get; init; }

    public string? NickName { get; init; }

    public string? AvatarUrl { get; init; }
}
