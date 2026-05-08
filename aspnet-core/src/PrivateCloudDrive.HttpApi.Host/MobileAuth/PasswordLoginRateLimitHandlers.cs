using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Volo.Abp;

namespace PrivateCloudDrive.MobileAuth;

public class PasswordLoginRateLimitValidationHandler :
    IOpenIddictServerHandler<OpenIddictServerEvents.ValidateTokenRequestContext>
{
    private readonly IPasswordLoginRateLimiter _rateLimiter;

    public PasswordLoginRateLimitValidationHandler(IPasswordLoginRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

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

public class PasswordLoginRateLimitResponseHandler :
    IOpenIddictServerHandler<OpenIddictServerEvents.ApplyTokenResponseContext>
{
    private readonly IPasswordLoginRateLimiter _rateLimiter;

    public PasswordLoginRateLimitResponseHandler(IPasswordLoginRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

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
