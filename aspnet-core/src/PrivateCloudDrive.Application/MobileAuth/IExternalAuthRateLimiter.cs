using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 第三方登录、绑定和解绑操作的限流服务。
/// </summary>
public interface IExternalAuthRateLimiter
{
    /// <summary>
    /// 检查指定操作和主体是否超过限流阈值；超限时抛出业务异常。
    /// </summary>
    Task CheckAsync(string operation, string subject);
}
