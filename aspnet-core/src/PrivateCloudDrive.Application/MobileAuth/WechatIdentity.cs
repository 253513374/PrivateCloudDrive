namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证WechatIdentity，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class WechatIdentity
{
    public string AppId { get; init; } = string.Empty;

    public string OpenId { get; init; } = string.Empty;

    public string? UnionId { get; init; }

    public string? NickName { get; init; }

    public string? AvatarUrl { get; init; }
}
