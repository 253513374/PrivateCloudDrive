using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using PrivateCloudDrive.MobileAuth;
using Volo.Abp.Identity;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace PrivateCloudDrive.MobileAuth;

public class WechatTokenGrantHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly IWechatLoginService _wechatLoginService;
    private readonly IdentityUserManager _userManager;
    private readonly IUserClaimsPrincipalFactory<AbpIdentityUser> _claimsPrincipalFactory;

    public WechatTokenGrantHandler(
        IWechatLoginService wechatLoginService,
        IdentityUserManager userManager,
        IUserClaimsPrincipalFactory<AbpIdentityUser> claimsPrincipalFactory)
    {
        _wechatLoginService = wechatLoginService;
        _userManager = userManager;
        _claimsPrincipalFactory = claimsPrincipalFactory;
    }

    public virtual async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!string.Equals(context.Request.GrantType, WechatLoginConsts.GrantType, System.StringComparison.Ordinal))
        {
            return;
        }

        var code = GetParameterString(context, "code");
        if (string.IsNullOrWhiteSpace(code))
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidRequest,
                description: "The WeChat authorization code is required.",
                uri: null);
            return;
        }

        var result = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = code,
            State = GetParameterString(context, "state"),
            Platform = GetParameterString(context, "platform"),
            DeviceIdHash = GetParameterString(context, "device_id"),
            ClientId = context.Request.ClientId
        });

        if (!result.Succeeded || !result.UserId.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(result.BindingTicket))
            {
                context.Parameters["binding_ticket"] = result.BindingTicket;
            }

            context.Reject(
                error: result.Error ?? OpenIddictConstants.Errors.InvalidGrant,
                description: result.ErrorDescription ?? "WeChat login failed.",
                uri: null);
            return;
        }

        var user = await _userManager.FindByIdAsync(result.UserId.Value.ToString());
        if (user == null || !user.IsActive)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidGrant,
                description: "The bound user cannot sign in.",
                uri: null);
            return;
        }

        var principal = await _claimsPrincipalFactory.CreateAsync(user);
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        principal.SetScopes(context.Request.GetScopes());
        principal.SetResources("PrivateCloudDrive");
        principal.SetDestinations(_ => new[]
        {
            OpenIddictConstants.Destinations.AccessToken,
            OpenIddictConstants.Destinations.IdentityToken
        });

        context.SignIn(
            principal,
            new Dictionary<string, OpenIddictParameter>
            {
                ["login_provider"] = WechatLoginConsts.ProviderName
            });
    }

    private static string? GetParameterString(
        OpenIddictServerEvents.HandleTokenRequestContext context,
        string name)
    {
        var value = context.Request.GetParameter(name).ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
