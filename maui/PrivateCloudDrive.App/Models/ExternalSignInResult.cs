namespace PrivateCloudDrive.App.Models;

/// <summary>
/// MAUI 第三方登录结果；未绑定时携带绑定票据供页面进入绑定流程。
/// </summary>
public sealed record ExternalSignInResult(
    bool Succeeded,
    bool BindingRequired,
    string? BindingTicket,
    string? ErrorMessage)
{
    /// <summary>
    /// 执行Success操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static ExternalSignInResult Success()
    {
        return new ExternalSignInResult(true, false, null, null);
    }

    /// <summary>
    /// 执行RequireBinding操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static ExternalSignInResult RequireBinding(string? bindingTicket, string? errorMessage)
    {
        return new ExternalSignInResult(false, true, bindingTicket, errorMessage);
    }
}
