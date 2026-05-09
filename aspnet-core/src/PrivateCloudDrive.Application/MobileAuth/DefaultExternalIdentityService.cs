using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 默认第三方身份解析服务。
/// 负责向 Google/GitHub 交换授权码并读取用户资料，返回系统内部统一的 ExternalIdentity。
/// </summary>
[ExposeServices(
    typeof(IExternalIdentityService),
    typeof(DefaultExternalIdentityService))]
public class DefaultExternalIdentityService :
    IExternalIdentityService,
    ITransientDependency
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ExternalLoginOptions _options;

    /// <summary>
    /// 初始化 <see cref="DefaultExternalIdentityService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DefaultExternalIdentityService(IOptions<ExternalLoginOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 根据 Provider 分派到对应的 OAuth/OIDC 交换流程。
    /// </summary>
    public virtual async Task<ExternalIdentity> ExchangeAsync(
        string provider,
        string code,
        string redirectUri,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = ExternalLoginConsts.NormalizeProvider(provider);
        if (normalizedProvider == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginProviderUnsupported)
                .WithData("error", ExternalLoginConsts.ProviderUnsupportedError);
        }

        var providerOptions = _options.GetProvider(normalizedProvider);
        var requireClientSecret = normalizedProvider == ExternalLoginConsts.GitHubProviderName;
        if (providerOptions == null || !providerOptions.IsUsable(requireClientSecret))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginDisabled)
                .WithData("error", ExternalLoginConsts.DisabledError);
        }

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds))
        };

        return normalizedProvider switch
        {
            ExternalLoginConsts.GoogleProviderName => await ExchangeGoogleAsync(
                httpClient,
                providerOptions,
                code,
                redirectUri,
                codeVerifier,
                cancellationToken),
            ExternalLoginConsts.GitHubProviderName => await ExchangeGitHubAsync(
                httpClient,
                providerOptions,
                code,
                redirectUri,
                codeVerifier,
                cancellationToken),
            _ => throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginProviderUnsupported)
                .WithData("error", ExternalLoginConsts.ProviderUnsupportedError)
        };
    }

    /// <summary>
    /// 使用 Google OIDC userinfo 接口获取 sub、email、name 和头像。
    /// </summary>
    private static async Task<ExternalIdentity> ExchangeGoogleAsync(
        HttpClient httpClient,
        ExternalLoginProviderOptions providerOptions,
        string code,
        string redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken)
    {
        var token = await ExchangeTokenAsync(
            httpClient,
            providerOptions,
            code,
            redirectUri,
            codeVerifier,
            ExternalLoginConsts.GoogleProviderName,
            cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, providerOptions.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var userInfo = await SendJsonAsync<GoogleUserInfoResponse>(
            httpClient,
            request,
            ExternalLoginConsts.GoogleProviderName,
            cancellationToken);

        if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Sub))
        {
            throw CreateExchangeFailedException(ExternalLoginConsts.GoogleProviderName);
        }

        return new ExternalIdentity
        {
            Provider = ExternalLoginConsts.GoogleProviderName,
            ProviderUserId = userInfo.Sub,
            Email = userInfo.Email,
            DisplayName = userInfo.Name,
            AvatarUrl = userInfo.Picture
        };
    }

    /// <summary>
    /// 使用 GitHub OAuth 接口获取用户资料；当公开资料无邮箱时再尝试读取已验证主邮箱。
    /// </summary>
    private static async Task<ExternalIdentity> ExchangeGitHubAsync(
        HttpClient httpClient,
        ExternalLoginProviderOptions providerOptions,
        string code,
        string redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken)
    {
        var token = await ExchangeTokenAsync(
            httpClient,
            providerOptions,
            code,
            redirectUri,
            codeVerifier,
            ExternalLoginConsts.GitHubProviderName,
            cancellationToken);

        using var request = CreateGitHubRequest(HttpMethod.Get, providerOptions.UserInfoEndpoint, token.AccessToken!);
        var userInfo = await SendJsonAsync<GitHubUserInfoResponse>(
            httpClient,
            request,
            ExternalLoginConsts.GitHubProviderName,
            cancellationToken);

        if (userInfo == null || userInfo.Id <= 0)
        {
            throw CreateExchangeFailedException(ExternalLoginConsts.GitHubProviderName);
        }

        var email = userInfo.Email;
        if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(providerOptions.EmailsEndpoint))
        {
            var emails = await TryGetGitHubEmailsAsync(
                httpClient,
                providerOptions.EmailsEndpoint,
                token.AccessToken!,
                cancellationToken);

            email = emails?
                .Where(item => item.Primary && item.Verified && !string.IsNullOrWhiteSpace(item.Email))
                .Select(item => item.Email)
                .FirstOrDefault();
        }

        return new ExternalIdentity
        {
            Provider = ExternalLoginConsts.GitHubProviderName,
            ProviderUserId = userInfo.Id.ToString(),
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(userInfo.Name) ? userInfo.Login : userInfo.Name,
            AvatarUrl = userInfo.AvatarUrl
        };
    }

    private static async Task<List<GitHubEmailResponse>?> TryGetGitHubEmailsAsync(
        HttpClient httpClient,
        string endpoint,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var emailsRequest = CreateGitHubRequest(HttpMethod.Get, endpoint, accessToken);
        using var response = await httpClient.SendAsync(emailsRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<List<GitHubEmailResponse>>(responseText, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 调用 Provider token endpoint 换取 access token。
    /// access token 只在当前请求内使用，不写入数据库、日志或响应。
    /// </summary>
    private static async Task<ExternalTokenResponse> ExchangeTokenAsync(
        HttpClient httpClient,
        ExternalLoginProviderOptions providerOptions,
        string code,
        string redirectUri,
        string? codeVerifier,
        string provider,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = providerOptions.ClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        };

        if (provider == ExternalLoginConsts.GoogleProviderName)
        {
            parameters["grant_type"] = "authorization_code";
        }

        if (!string.IsNullOrWhiteSpace(providerOptions.ClientSecret))
        {
            parameters["client_secret"] = providerOptions.ClientSecret;
        }

        if (!string.IsNullOrWhiteSpace(codeVerifier))
        {
            parameters["code_verifier"] = codeVerifier;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, providerOptions.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(parameters)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (provider == ExternalLoginConsts.GitHubProviderName)
        {
            request.Headers.UserAgent.ParseAdd("PrivateCloudDrive/1.0");
        }

        var token = await SendJsonAsync<ExternalTokenResponse>(
            httpClient,
            request,
            provider,
            cancellationToken);

        if (token == null ||
            !string.IsNullOrWhiteSpace(token.Error) ||
            string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw CreateExchangeFailedException(provider, token?.Error, token?.ErrorDescription);
        }

        return token;
    }

    private static HttpRequestMessage CreateGitHubRequest(
        HttpMethod method,
        string url,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("PrivateCloudDrive/1.0");
        return request;
    }

    /// <summary>
    /// 发送 HTTP 请求并反序列化 JSON 响应，同时把 Provider 错误归一化为业务异常。
    /// </summary>
    private static async Task<T?> SendJsonAsync<T>(
        HttpClient httpClient,
        HttpRequestMessage request,
        string provider,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var providerError = ExtractProviderError(responseText, response.StatusCode);
                throw CreateExchangeFailedException(
                    provider,
                    providerError.Error,
                    providerError.Description,
                    providerError.Status);
            }

            return JsonSerializer.Deserialize<T>(responseText, JsonOptions);
        }
        catch (HttpRequestException exception)
        {
            throw CreateExchangeFailedException(
                provider,
                "http_request_failed",
                NormalizeProviderMessage(exception.Message));
        }
        catch (TaskCanceledException exception)
        {
            throw CreateExchangeFailedException(
                provider,
                "request_timeout",
                NormalizeProviderMessage(exception.Message));
        }
        catch (JsonException exception)
        {
            throw CreateExchangeFailedException(
                provider,
                "invalid_json",
                NormalizeProviderMessage(exception.Message));
        }
    }

    private static BusinessException CreateExchangeFailedException(
        string provider,
        string? providerError = null,
        string? providerErrorDescription = null,
        string? providerStatus = null)
    {
        var exception = new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginCodeExchangeFailed)
            .WithData("error", ExternalLoginConsts.CodeExchangeFailedError)
            .WithData("provider", provider);

        if (!string.IsNullOrWhiteSpace(providerError))
        {
            exception.WithData("provider_error", providerError);
        }

        if (!string.IsNullOrWhiteSpace(providerErrorDescription))
        {
            exception.WithData("provider_error_description", providerErrorDescription);
        }

        if (!string.IsNullOrWhiteSpace(providerStatus))
        {
            exception.WithData("provider_status", providerStatus);
        }

        return exception;
    }

    private static ProviderError ExtractProviderError(string responseText, HttpStatusCode statusCode)
    {
        var status = $"{(int)statusCode} {statusCode}";
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new ProviderError($"http_{(int)statusCode}", $"HTTP {status}", status);
        }

        try
        {
            var response = JsonSerializer.Deserialize<ProviderErrorResponse>(responseText, JsonOptions);
            var error = FirstNonEmpty(response?.Error, response?.Message, $"http_{(int)statusCode}");
            var description = FirstNonEmpty(
                response?.ErrorDescription,
                response?.Message,
                NormalizeProviderMessage(responseText));

            return new ProviderError(error, $"HTTP {status}: {description}", status);
        }
        catch (JsonException)
        {
            return new ProviderError(
                $"http_{(int)statusCode}",
                $"HTTP {status}: {NormalizeProviderMessage(responseText)}",
                status);
        }
    }

    private static string NormalizeProviderMessage(string? value)
    {
        var message = value?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return "No provider response body.";
        }

        return message.Length <= 512 ? message : message[..512];
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private sealed record ProviderError(string Error, string Description, string Status);

    private sealed class ExternalTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }

    private sealed class ProviderErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    private sealed class GoogleUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("picture")]
        public string? Picture { get; init; }
    }

    private sealed class GitHubUserInfoResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("login")]
        public string? Login { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; init; }
    }

    private sealed class GitHubEmailResponse
    {
        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("primary")]
        public bool Primary { get; init; }

        [JsonPropertyName("verified")]
        public bool Verified { get; init; }
    }
}
