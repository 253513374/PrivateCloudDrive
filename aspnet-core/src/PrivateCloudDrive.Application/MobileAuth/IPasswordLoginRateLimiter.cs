using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证IPasswordLoginRateLimiter，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public interface IPasswordLoginRateLimiter
{
    Task CheckAsync(string? userName, string? ipAddress);

    Task RecordFailureAsync(string? userName, string? ipAddress);

    Task ResetUserAsync(string? userName);
}
