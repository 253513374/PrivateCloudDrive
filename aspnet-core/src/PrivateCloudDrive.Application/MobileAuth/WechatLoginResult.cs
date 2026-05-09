using System;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示WechatLoginResult操作结果，用于向调用方返回处理状态和必要业务信息。
/// </summary>
public class WechatLoginResult
{
    public bool Succeeded { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string? Error { get; init; }

    public string? ErrorDescription { get; init; }

    public string? BindingTicket { get; init; }

    /// <summary>
    /// 执行Success操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static WechatLoginResult Success(Guid userId, string? userName)
    {
        return new WechatLoginResult
        {
            Succeeded = true,
            UserId = userId,
            UserName = userName
        };
    }

    /// <summary>
    /// 执行Failure操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static WechatLoginResult Failure(string error, string description, string? bindingTicket = null)
    {
        return new WechatLoginResult
        {
            Error = error,
            ErrorDescription = description,
            BindingTicket = bindingTicket
        };
    }
}
