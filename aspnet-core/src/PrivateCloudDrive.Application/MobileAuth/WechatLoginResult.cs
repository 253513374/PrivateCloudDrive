using System;

namespace PrivateCloudDrive.MobileAuth;

public class WechatLoginResult
{
    public bool Succeeded { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string? Error { get; init; }

    public string? ErrorDescription { get; init; }

    public string? BindingTicket { get; init; }

    public static WechatLoginResult Success(Guid userId, string? userName)
    {
        return new WechatLoginResult
        {
            Succeeded = true,
            UserId = userId,
            UserName = userName
        };
    }

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
