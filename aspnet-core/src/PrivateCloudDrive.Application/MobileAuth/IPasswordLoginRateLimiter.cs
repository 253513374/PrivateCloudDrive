using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

public interface IPasswordLoginRateLimiter
{
    Task CheckAsync(string? userName, string? ipAddress);

    Task RecordFailureAsync(string? userName, string? ipAddress);

    Task ResetUserAsync(string? userName);
}
