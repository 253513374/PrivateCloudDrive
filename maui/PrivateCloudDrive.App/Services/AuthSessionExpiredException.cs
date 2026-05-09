namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示本地登录状态已失效，需要重新登录。
/// </summary>
public sealed class AuthSessionExpiredException : InvalidOperationException
{
    public AuthSessionExpiredException(string message)
        : base(message)
    {
    }
}
