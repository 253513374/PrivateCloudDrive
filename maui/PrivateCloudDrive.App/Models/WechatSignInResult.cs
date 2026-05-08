namespace PrivateCloudDrive.App.Models;

public sealed record WechatSignInResult(
    bool Succeeded,
    bool BindingRequired,
    string? BindingTicket,
    string? ErrorMessage)
{
    public static WechatSignInResult Success()
    {
        return new WechatSignInResult(true, false, null, null);
    }

    public static WechatSignInResult RequireBinding(string? bindingTicket, string? errorMessage)
    {
        return new WechatSignInResult(false, true, bindingTicket, errorMessage);
    }
}
