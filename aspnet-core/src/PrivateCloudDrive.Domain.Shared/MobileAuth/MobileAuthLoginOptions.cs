namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示MobileAuthLogin配置选项，用于集中管理运行时可调整参数。
/// </summary>
public class MobileAuthLoginOptions
{
    public bool EnablePasswordLoginRateLimit { get; set; } = true;

    public int MaxFailedAttempts { get; set; } = 5;

    public int WindowMinutes { get; set; } = 15;
}
