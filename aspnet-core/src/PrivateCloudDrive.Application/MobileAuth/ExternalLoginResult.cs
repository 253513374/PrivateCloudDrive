using System;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 第三方登录处理结果。
/// 登录成功时携带本地用户信息；未绑定或失败时携带标准错误码和可选绑定票据。
/// </summary>
public class ExternalLoginResult
{
    public bool Succeeded { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string? Error { get; init; }

    public string? ErrorDescription { get; init; }

    public string? BindingTicket { get; init; }

    /// <summary>
    /// 创建可签发本地令牌的成功结果。
    /// </summary>
    public static ExternalLoginResult Success(Guid userId, string? userName)
    {
        return new ExternalLoginResult
        {
            Succeeded = true,
            UserId = userId,
            UserName = userName
        };
    }

    /// <summary>
    /// 创建失败结果；当需要用户绑定已有账号时可携带 bindingTicket。
    /// </summary>
    public static ExternalLoginResult Failure(string error, string description, string? bindingTicket = null)
    {
        return new ExternalLoginResult
        {
            Error = error,
            ErrorDescription = description,
            BindingTicket = bindingTicket
        };
    }
}
