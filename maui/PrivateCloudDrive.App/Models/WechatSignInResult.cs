namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示WechatSignInResult操作结果，用于向调用方返回处理状态和必要业务信息。
/// </summary>
public sealed record WechatSignInResult(
    bool Succeeded,
    bool BindingRequired,
    string? BindingTicket,
    string? ErrorMessage)
{
    /// <summary>
    /// 执行Success操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static WechatSignInResult Success()
    {
        return new WechatSignInResult(true, false, null, null);
    }

    /// <summary>
    /// 执行RequireBinding操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static WechatSignInResult RequireBinding(string? bindingTicket, string? errorMessage)
    {
        return new WechatSignInResult(false, true, bindingTicket, errorMessage);
    }
}
