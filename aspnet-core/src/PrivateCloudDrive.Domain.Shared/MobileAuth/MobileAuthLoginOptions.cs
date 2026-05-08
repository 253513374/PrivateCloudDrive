namespace PrivateCloudDrive.MobileAuth;

public class MobileAuthLoginOptions
{
    public bool EnablePasswordLoginRateLimit { get; set; } = true;

    public int MaxFailedAttempts { get; set; } = 5;

    public int WindowMinutes { get; set; } = 15;
}
