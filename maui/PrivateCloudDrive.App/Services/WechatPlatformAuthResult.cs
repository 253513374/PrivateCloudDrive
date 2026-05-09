namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示WechatPlatformAuthResult操作结果，用于向调用方返回处理状态和必要业务信息。
/// </summary>
public sealed record WechatPlatformAuthResult(
    bool Succeeded,
    string? Code,
    string? State,
    string? Platform,
    string? ErrorMessage)
{
    /// <summary>
    /// 执行Failure操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static WechatPlatformAuthResult Failure(string message)
    {
        return new WechatPlatformAuthResult(false, null, null, null, message);
    }
}
