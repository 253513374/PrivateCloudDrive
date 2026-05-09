using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Volo.Abp;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证PasswordLoginRateLimitValidationHandler，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class PasswordLoginRateLimitValidationHandler :
    IOpenIddictServerHandler<OpenIddictServerEvents.ValidateTokenRequestContext>
{
    private readonly IPasswordLoginRateLimiter _rateLimiter;

    /// <summary>
    /// 初始化 <see cref="PasswordLoginRateLimitValidationHandler"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public PasswordLoginRateLimitValidationHandler(IPasswordLoginRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    /// <summary>
    /// 执行Handle操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public virtual async ValueTask HandleAsync(OpenIddictServerEvents.ValidateTokenRequestContext context)
    {
        if (!IsPasswordGrant(context.Request))
        {
            return;
        }

        try
        {
            await _rateLimiter.CheckAsync(
                GetParameterString(context, OpenIddictConstants.Parameters.Username),
                GetIpAddress(context));
        }
        catch (BusinessException exception) when (exception.Code == PrivateCloudDriveDomainErrorCodes.PasswordLoginRateLimited)
        {
            context.Reject(
                error: PasswordLoginConsts.RateLimitedError,
                description: "Too many failed login attempts. Try again later.",
                uri: null);
        }
    }

    private static bool IsPasswordGrant(OpenIddictRequest? request)
    {
        return string.Equals(
            request?.GrantType,
            OpenIddictConstants.GrantTypes.Password,
            StringComparison.Ordinal);
    }

    private static string? GetParameterString(
        OpenIddictServerEvents.ValidateTokenRequestContext context,
        string name)
    {
        var value = context.Request.GetParameter(name).ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetIpAddress(OpenIddictServerEvents.ValidateTokenRequestContext context)
    {
        var request = OpenIddictServerAspNetCoreHelpers.GetHttpRequest(context.Transaction);
        return request?.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// 表示移动认证PasswordLoginRateLimitResponseHandler，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class PasswordLoginRateLimitResponseHandler :
    IOpenIddictServerHandler<OpenIddictServerEvents.ApplyTokenResponseContext>
{
    private readonly IPasswordLoginRateLimiter _rateLimiter;

    /// <summary>
    /// 初始化 <see cref="PasswordLoginRateLimitResponseHandler"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public PasswordLoginRateLimitResponseHandler(IPasswordLoginRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    /// <summary>
    /// 执行Handle操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public virtual async ValueTask HandleAsync(OpenIddictServerEvents.ApplyTokenResponseContext context)
    {
        if (!IsPasswordGrant(context.Request))
        {
            return;
        }

        var userName = GetParameterString(context, OpenIddictConstants.Parameters.Username);
        if (string.IsNullOrWhiteSpace(context.Response?.Error))
        {
            await _rateLimiter.ResetUserAsync(userName);
            return;
        }

        if (string.Equals(context.Response.Error, OpenIddictConstants.Errors.InvalidGrant, StringComparison.Ordinal))
        {
            await _rateLimiter.RecordFailureAsync(userName, GetIpAddress(context));
        }
    }

    private static bool IsPasswordGrant(OpenIddictRequest? request)
    {
        return string.Equals(
            request?.GrantType,
            OpenIddictConstants.GrantTypes.Password,
            StringComparison.Ordinal);
    }

    private static string? GetParameterString(
        OpenIddictServerEvents.ApplyTokenResponseContext context,
        string name)
    {
        var value = context.Request?.GetParameter(name).ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetIpAddress(OpenIddictServerEvents.ApplyTokenResponseContext context)
    {
        var request = OpenIddictServerAspNetCoreHelpers.GetHttpRequest(context.Transaction);
        return request?.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
