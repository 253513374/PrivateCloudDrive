using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.MobileAuth;

[ExposeServices(
    typeof(IWechatIdentityService),
    typeof(DefaultWechatIdentityService))]
public class DefaultWechatIdentityService :
    IWechatIdentityService,
    ITransientDependency
{
    private readonly WechatLoginOptions _options;

    public DefaultWechatIdentityService(IOptions<WechatLoginOptions> options)
    {
        _options = options.Value;
    }

    public virtual async Task<WechatIdentity> ExchangeAsync(
        string code,
        string? platform = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsUsable())
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatDisabled)
                .WithData("error", WechatLoginConsts.DisabledError);
        }

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds))
        };

        var tokenUrl =
            "https://api.weixin.qq.com/sns/oauth2/access_token" +
            $"?appid={Uri.EscapeDataString(_options.AppId)}" +
            $"&secret={Uri.EscapeDataString(_options.AppSecret)}" +
            $"&code={Uri.EscapeDataString(code)}" +
            "&grant_type=authorization_code";

        var tokenResponse = await GetJsonAsync<WechatAccessTokenResponse>(
            httpClient,
            tokenUrl,
            cancellationToken);

        if (tokenResponse == null ||
            tokenResponse.ErrCode.HasValue ||
            string.IsNullOrWhiteSpace(tokenResponse.OpenId) ||
            string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw CreateExchangeFailedException(tokenResponse?.ErrCode, tokenResponse?.ErrMsg);
        }

        var identity = new WechatIdentity
        {
            AppId = _options.AppId,
            OpenId = tokenResponse.OpenId,
            UnionId = tokenResponse.UnionId
        };

        var userInfoUrl =
            "https://api.weixin.qq.com/sns/userinfo" +
            $"?access_token={Uri.EscapeDataString(tokenResponse.AccessToken)}" +
            $"&openid={Uri.EscapeDataString(tokenResponse.OpenId)}";

        var userInfo = await GetJsonAsync<WechatUserInfoResponse>(
            httpClient,
            userInfoUrl,
            cancellationToken);

        if (userInfo == null || userInfo.ErrCode.HasValue)
        {
            return identity;
        }

        return new WechatIdentity
        {
            AppId = _options.AppId,
            OpenId = tokenResponse.OpenId,
            UnionId = string.IsNullOrWhiteSpace(userInfo.UnionId) ? tokenResponse.UnionId : userInfo.UnionId,
            NickName = userInfo.NickName,
            AvatarUrl = userInfo.HeadImgUrl
        };
    }

    private static async Task<T?> GetJsonAsync<T>(
        HttpClient httpClient,
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var responseText = await httpClient.GetStringAsync(url, cancellationToken);
            return JsonSerializer.Deserialize<T>(responseText);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatCodeExchangeFailed)
                .WithData("error", WechatLoginConsts.CodeExchangeFailedError);
        }
    }

    private static BusinessException CreateExchangeFailedException(int? errCode, string? errMsg)
    {
        var exception = new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatCodeExchangeFailed)
            .WithData("error", WechatLoginConsts.CodeExchangeFailedError);

        if (errCode.HasValue)
        {
            exception.WithData("wechat_errcode", errCode.Value);
        }

        if (!string.IsNullOrWhiteSpace(errMsg))
        {
            exception.WithData("wechat_error", errMsg);
        }

        return exception;
    }

    private sealed class WechatAccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("openid")]
        public string? OpenId { get; init; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }

        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; init; }
    }

    private sealed class WechatUserInfoResponse
    {
        [JsonPropertyName("nickname")]
        public string? NickName { get; init; }

        [JsonPropertyName("headimgurl")]
        public string? HeadImgUrl { get; init; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; init; }

        [JsonPropertyName("errcode")]
        public int? ErrCode { get; init; }
    }
}
