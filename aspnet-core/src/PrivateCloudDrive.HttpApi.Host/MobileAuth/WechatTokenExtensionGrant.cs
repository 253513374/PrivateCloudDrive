using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证WechatTokenExtensionGrant，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class WechatTokenExtensionGrant : ITokenExtensionGrant
{
    public string Name => WechatLoginConsts.GrantType;

    /// <summary>
    /// 执行Handle操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public virtual async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var code = GetParameterString(context, "code");
        if (string.IsNullOrWhiteSpace(code))
        {
            return Forbid(OpenIddictConstants.Errors.InvalidRequest, "The WeChat authorization code is required.");
        }

        var wechatLoginService = context.HttpContext.RequestServices.GetRequiredService<IWechatLoginService>();
        var result = await wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = code,
            State = GetParameterString(context, "state"),
            Platform = GetParameterString(context, "platform"),
            DeviceIdHash = GetParameterString(context, "device_id"),
            ClientId = context.Request.ClientId
        });

        if (!result.Succeeded || !result.UserId.HasValue)
        {
            return Forbid(
                result.Error ?? OpenIddictConstants.Errors.InvalidGrant,
                result.ErrorDescription ?? "WeChat login failed.",
                new Dictionary<string, string?>
                {
                    ["binding_ticket"] = result.BindingTicket
                });
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var user = await userManager.FindByIdAsync(result.UserId.Value.ToString());
        if (user == null || !user.IsActive)
        {
            return Forbid(OpenIddictConstants.Errors.InvalidGrant, "The bound user cannot sign in.");
        }

        var principal = await CreatePrincipalAsync(context, user);
        return new Microsoft.AspNetCore.Mvc.SignInResult(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            principal);
    }

    private static async Task<System.Security.Claims.ClaimsPrincipal> CreatePrincipalAsync(
        ExtensionGrantContext context,
        AbpIdentityUser user)
    {
        var claimsPrincipalFactory = context.HttpContext.RequestServices
            .GetRequiredService<IUserClaimsPrincipalFactory<AbpIdentityUser>>();
        var principalManager = context.HttpContext.RequestServices
            .GetRequiredService<AbpOpenIddictClaimsPrincipalManager>();

        var principal = await claimsPrincipalFactory.CreateAsync(user);
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        principal.SetScopes(context.Request.GetScopes());
        principal.SetResources("PrivateCloudDrive");
        principal.SetDestinations(_ => new[]
        {
            OpenIddictConstants.Destinations.AccessToken,
            OpenIddictConstants.Destinations.IdentityToken
        });

        await principalManager.HandleAsync(context.Request, principal);
        return principal;
    }

    private static ForbidResult Forbid(
        string error,
        string description,
        IDictionary<string, string?>? extraParameters = null)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });

        if (extraParameters != null)
        {
            foreach (var parameter in extraParameters)
            {
                if (!string.IsNullOrWhiteSpace(parameter.Value))
                {
                    properties.SetParameter(parameter.Key, parameter.Value);
                }
            }
        }

        return new ForbidResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, properties);
    }

    private static string? GetParameterString(ExtensionGrantContext context, string name)
    {
        var value = context.Request.GetParameter(name).ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
