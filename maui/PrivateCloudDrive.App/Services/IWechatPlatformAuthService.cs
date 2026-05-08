using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public interface IWechatPlatformAuthService
{
    Task<bool> IsAvailableAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default);

    Task<WechatPlatformAuthResult> AuthorizeAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default);
}
