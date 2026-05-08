using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public sealed class DefaultWechatPlatformAuthService : IWechatPlatformAuthService
{
    public Task<bool> IsAvailableAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<WechatPlatformAuthResult> AuthorizeAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            WechatPlatformAuthResult.Failure("WeChat authorization is not available in this build."));
    }
}
