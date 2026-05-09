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
/// OpenIddict 自定义第三方登录 grant。
/// MAUI 客户端提交 Provider 授权码后，由该 grant 调用应用服务并最终签发本系统 Token。
/// </summary>
public class ExternalTokenExtensionGrant : ITokenExtensionGrant
{
    public string Name => ExternalLoginConsts.GrantType;

    /// <summary>
    /// 处理 urn:privateclouddrive:external token 请求。
    /// 未绑定时返回 invalid_grant 和 binding_ticket，引导客户端进入绑定已有账号流程。
    /// </summary>
    public virtual async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var provider = GetParameterString(context, "provider");
        var code = GetParameterString(context, "code");
        var redirectUri = GetParameterString(context, "redirect_uri");
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Forbid(OpenIddictConstants.Errors.InvalidRequest, "The external login provider is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Forbid(OpenIddictConstants.Errors.InvalidRequest, "The external authorization code is required.");
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return Forbid(OpenIddictConstants.Errors.InvalidRequest, "The external redirect_uri is required.");
        }

        var externalLoginService = context.HttpContext.RequestServices.GetRequiredService<IExternalLoginService>();
        var result = await externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = provider,
            Code = code,
            State = GetParameterString(context, "state"),
            RedirectUri = redirectUri,
            CodeVerifier = GetParameterString(context, "code_verifier"),
            DeviceIdHash = GetParameterString(context, "device_id"),
            ClientId = context.Request.ClientId
        });

        if (!result.Succeeded || !result.UserId.HasValue)
        {
            return Forbid(
                result.Error ?? OpenIddictConstants.Errors.InvalidGrant,
                result.ErrorDescription ?? "External login failed.",
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

    /// <summary>
    /// 创建携带 OpenIddict scopes/resources/destinations 的 ClaimsPrincipal。
    /// </summary>
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
