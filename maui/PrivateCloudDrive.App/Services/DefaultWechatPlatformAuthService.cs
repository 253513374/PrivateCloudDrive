using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 提供DefaultWechatPlatformAuth服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public sealed class DefaultWechatPlatformAuthService : IWechatPlatformAuthService
{
    /// <summary>
    /// 执行IsAvailable操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public Task<bool> IsAvailableAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// 执行Authorize操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public Task<WechatPlatformAuthResult> AuthorizeAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            WechatPlatformAuthResult.Failure("WeChat authorization is not available in this build."));
    }
}
