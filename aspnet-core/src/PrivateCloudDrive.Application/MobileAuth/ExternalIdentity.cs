namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 从第三方 Provider 解析出的稳定用户身份。
/// 该模型只保存业务绑定所需的公开资料，不保存授权码、access token 或 refresh token。
/// </summary>
public class ExternalIdentity
{
    public string Provider { get; init; } = string.Empty;

    public string ProviderUserId { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public string? AvatarUrl { get; init; }
}
