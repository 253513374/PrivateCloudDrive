using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 提供IWechatPlatformAuth服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public interface IWechatPlatformAuthService
{
    Task<bool> IsAvailableAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default);

    Task<WechatPlatformAuthResult> AuthorizeAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default);
}
