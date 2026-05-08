using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

public interface IWechatAuthRateLimiter
{
    Task CheckAsync(string operation, string subject);
}
