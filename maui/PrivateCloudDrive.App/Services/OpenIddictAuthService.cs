using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 基于 OpenIddict 的 MAUI 认证服务实现。
/// 负责 password grant、authorization code + PKCE、微信扩展 grant、Google/GitHub 外部 grant 以及安全存储 Token。
/// </summary>
public sealed class OpenIddictAuthService : IAuthService
{
    private const string TokenStorageKey = "auth.tokens";
    private const string WechatGrantType = "urn:privateclouddrive:wechat";
    private const string WechatBindingRequiredError = "wechat_binding_required";
    private const string ExternalGrantType = "urn:privateclouddrive:external";
    private const string ExternalBindingRequiredError = "external_binding_required";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan BrowserAuthenticationTimeout = TimeSpan.FromSeconds(45);

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(AppSettings.ApiBaseUrl)
    };

    /// <summary>
    /// 检查本地是否存在可用 access token。
    /// </summary>
    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default)
    {
        return !string.IsNullOrWhiteSpace(await GetAccessTokenAsync(cancellationToken));
    }

    /// <summary>
    /// 使用账号密码登录；登录失败会清理本地 Token，避免继续使用旧会话。
    /// </summary>
    public async Task SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        await SignOutInternalAsync(recordAudit: false, cancellationToken);

        try
        {
            var tokenResponse = await RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = AppSettings.OAuthClientId,
                    ["username"] = userName,
                    ["password"] = password,
                    ["scope"] = AppSettings.OAuthScopes
                },
                cancellationToken);

            await SaveTokenSetAsync(tokenResponse, cancellationToken);
            await RecordAuditAsync(
                "Password",
                "PasswordLogin",
                "Success",
                failureReason: null,
                userName,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await SignOutInternalAsync(recordAudit: false, cancellationToken);
            await RecordAuditAsync(
                "Password",
                "PasswordLogin",
                "Failed",
                NormalizeAuditReason(exception.Message),
                userName,
                cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 执行登录流程，统一处理身份校验、绑定状态、安全审计和错误返回。
    /// </summary>
    public async Task<WechatSignInResult> SignInWithWechatCodeAsync(
        string code,
        string? state,
        string? platform,
        string? deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("WeChat did not return an authorization code.");
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = WechatGrantType,
                ["client_id"] = AppSettings.OAuthClientId,
                ["code"] = code.Trim(),
                ["scope"] = AppSettings.OAuthScopes
            };

            AddOptionalParameter(parameters, "state", state);
            AddOptionalParameter(parameters, "platform", platform);
            AddOptionalParameter(parameters, "device_id", deviceIdHash);

            var tokenResponse = await RequestTokenAsync(parameters, cancellationToken);
            await SaveTokenSetAsync(tokenResponse, cancellationToken);

            return WechatSignInResult.Success();
        }
        catch (OAuthTokenException exception) when (exception.Error == WechatBindingRequiredError)
        {
            return WechatSignInResult.RequireBinding(exception.BindingTicket, exception.Message);
        }
    }

    /// <summary>
    /// 发起第三方 Provider 授权，生成 state 和可选 PKCE challenge，并验证回调结果。
    /// </summary>
    public async Task<ExternalAuthorizationResult> AuthorizeExternalAsync(
        ExternalLoginProviderSettings provider,
        CancellationToken cancellationToken = default)
    {
        if (!provider.IsEnabled ||
            string.IsNullOrWhiteSpace(provider.ClientId) ||
            string.IsNullOrWhiteSpace(provider.AuthorizationEndpoint) ||
            string.IsNullOrWhiteSpace(provider.RedirectUri))
        {
            throw new InvalidOperationException("External sign-in provider is not enabled.");
        }

        var state = CreateBase64UrlRandom(32);
        var codeVerifier = provider.UsePkce ? CreateBase64UrlRandom(32) : null;
        var codeChallenge = string.IsNullOrWhiteSpace(codeVerifier)
            ? null
            : CreateCodeChallenge(codeVerifier);
        var redirectUri = GetExternalRedirectUri(provider.RedirectUri);
        var callbackUrl = new Uri(redirectUri);
        var authorizationUrl = BuildExternalAuthorizeUri(provider, state, codeChallenge, redirectUri);

        var result = await AuthenticateAsync(authorizationUrl, callbackUrl, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (result.TryGetValue("error", out var error))
        {
            throw new InvalidOperationException(error);
        }

        if (!result.TryGetValue("state", out var returnedState) ||
            returnedState != state)
        {
            throw new InvalidOperationException("Invalid external sign-in state.");
        }

        if (!result.TryGetValue("code", out var authorizationCode) ||
            string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new InvalidOperationException("External provider did not return an authorization code.");
        }

        return new ExternalAuthorizationResult(
            provider.Provider,
            authorizationCode,
            returnedState,
            redirectUri,
            codeVerifier);
    }

    /// <summary>
    /// 将第三方授权结果提交给后端扩展 grant；未绑定时返回绑定票据而不是抛出普通登录失败。
    /// </summary>
    public async Task<ExternalSignInResult> SignInWithExternalCodeAsync(
        string provider,
        string code,
        string? state,
        string redirectUri,
        string? codeVerifier,
        string? deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("External sign-in provider and authorization code are required.");
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = ExternalGrantType,
                ["client_id"] = AppSettings.OAuthClientId,
                ["provider"] = provider.Trim(),
                ["code"] = code.Trim(),
                ["redirect_uri"] = redirectUri,
                ["scope"] = AppSettings.OAuthScopes
            };

            AddOptionalParameter(parameters, "state", state);
            AddOptionalParameter(parameters, "code_verifier", codeVerifier);
            AddOptionalParameter(parameters, "device_id", deviceIdHash);

            var tokenResponse = await RequestTokenAsync(parameters, cancellationToken);
            await SaveTokenSetAsync(tokenResponse, cancellationToken);

            return ExternalSignInResult.Success();
        }
        catch (OAuthTokenException exception) when (exception.Error == ExternalBindingRequiredError)
        {
            return ExternalSignInResult.RequireBinding(exception.BindingTicket, exception.Message);
        }
    }

    /// <summary>
    /// 使用系统浏览器完成本系统 OpenIddict authorization code + PKCE 登录。
    /// </summary>
    public async Task SignInWithBrowserAsync(CancellationToken cancellationToken = default)
    {
        var state = CreateBase64UrlRandom(32);
        var codeVerifier = CreateBase64UrlRandom(32);
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var redirectUri = GetRedirectUri();
        var callbackUrl = new Uri(redirectUri);
        var authorizationUrl = BuildAuthorizeUri(state, codeChallenge, redirectUri);

        var result = await AuthenticateAsync(authorizationUrl, callbackUrl, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (result.TryGetValue("error", out var error))
        {
            throw new InvalidOperationException(error);
        }

        if (!result.TryGetValue("state", out var returnedState) ||
            returnedState != state)
        {
            throw new InvalidOperationException("Invalid OpenIddict sign-in state.");
        }

        if (!result.TryGetValue("code", out var authorizationCode) ||
            string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new InvalidOperationException("OpenIddict did not return an authorization code.");
        }

        var tokenResponse = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = AppSettings.OAuthClientId,
                ["redirect_uri"] = redirectUri,
                ["code"] = authorizationCode,
                ["code_verifier"] = codeVerifier
            },
            cancellationToken);

        await SaveTokenSetAsync(tokenResponse, cancellationToken);
    }

    /// <summary>
    /// 执行SignOut操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await SignOutInternalAsync(recordAudit: true, cancellationToken);
    }

    private async Task SignOutInternalAsync(bool recordAudit, CancellationToken cancellationToken)
    {
        var tokenSet = await LoadTokenSetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(tokenSet?.RefreshToken))
        {
            try
            {
                using var content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["client_id"] = AppSettings.OAuthClientId,
                        ["token"] = tokenSet.RefreshToken,
                        ["token_type_hint"] = "refresh_token"
                    });

                EnsureBaseAddressCurrent();
                using var response = await _httpClient.PostAsync("connect/revocation", content, cancellationToken);
                _ = response;
            }
            catch
            {
                // Local sign-out must clear tokens even if revocation cannot reach the server.
            }
        }

        SecureStorage.Default.Remove(TokenStorageKey);

        if (recordAudit)
        {
            await RecordAuditAsync(
                "Password",
                "Logout",
                "Success",
                failureReason: null,
                userName: null,
                cancellationToken);
        }
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokenSet = await LoadTokenSetAsync(cancellationToken);
        if (tokenSet == null)
        {
            return null;
        }

        if (tokenSet.ExpiresAt > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return tokenSet.AccessToken;
        }

        if (string.IsNullOrWhiteSpace(tokenSet.RefreshToken))
        {
            await SignOutAsync(cancellationToken);
            return null;
        }

        try
        {
            var refreshedToken = await RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = AppSettings.OAuthClientId,
                    ["refresh_token"] = tokenSet.RefreshToken
                },
                cancellationToken);

            await SaveTokenSetAsync(refreshedToken, cancellationToken);
            return refreshedToken.AccessToken;
        }
        catch (Exception exception)
        {
            await RecordAuditAsync(
                "RefreshToken",
                "RefreshToken",
                "Failed",
                NormalizeAuditReason(exception.Message),
                userName: null,
                cancellationToken);
            await SignOutInternalAsync(recordAudit: false, cancellationToken);
            return null;
        }
    }

    private static string GetRedirectUri()
    {
#if WINDOWS
        return AppSettings.WindowsOAuthRedirectUri;
#else
        return AppSettings.OAuthRedirectUri;
#endif
    }

    private static string GetExternalRedirectUri(string providerRedirectUri)
    {
#if WINDOWS
        return AppSettings.WindowsOAuthRedirectUri;
#else
        return providerRedirectUri;
#endif
    }

    private static async Task<IReadOnlyDictionary<string, string>> AuthenticateAsync(
        Uri authorizationUrl,
        Uri callbackUrl,
        CancellationToken cancellationToken)
    {
#if WINDOWS
        return await AuthenticateWithLoopbackRedirectAsync(authorizationUrl, callbackUrl, cancellationToken);
#else
        var authenticationTask = WebAuthenticator.Default.AuthenticateAsync(authorizationUrl, callbackUrl);
        var timeoutTask = Task.Delay(BrowserAuthenticationTimeout, cancellationToken);
        var completedTask = await Task.WhenAny(authenticationTask, timeoutTask);

        if (completedTask != authenticationTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = authenticationTask.ContinueWith(
                task => _ = task.Exception,
                TaskContinuationOptions.OnlyOnFaulted);
            throw new TimeoutException("External browser sign-in timed out.");
        }

        var result = await authenticationTask;
        return result.Properties;
#endif
    }

#if WINDOWS
    private static async Task<IReadOnlyDictionary<string, string>> AuthenticateWithLoopbackRedirectAsync(
        Uri authorizationUrl,
        Uri callbackUrl,
        CancellationToken cancellationToken)
    {
        using var listener = new TcpListener(IPAddress.Loopback, callbackUrl.Port);
        listener.Start();

        var browserOpened = await Launcher.Default.OpenAsync(authorizationUrl);
        if (!browserOpened)
        {
            throw new InvalidOperationException("Unable to open the system browser for sign-in.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));

        using var client = await listener.AcceptTcpClientAsync(timeout.Token);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

        var requestLine = await reader.ReadLineAsync(timeout.Token);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            throw new InvalidOperationException("The sign-in callback did not contain an HTTP request.");
        }

        string? headerLine;
        do
        {
            headerLine = await reader.ReadLineAsync(timeout.Token);
        }
        while (!string.IsNullOrEmpty(headerLine));

        var requestUri = ParseLoopbackRequestUri(requestLine, callbackUrl);
        var properties = ParseQueryString(requestUri.Query);
        var response = BuildLoopbackBrowserResponse(properties.ContainsKey("error"));
        var responseBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBytes, timeout.Token);

        return properties;
    }

    private static Uri ParseLoopbackRequestUri(string requestLine, Uri callbackUrl)
    {
        var requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2 || !string.Equals(requestParts[0], "GET", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The sign-in callback used an invalid HTTP request.");
        }

        var requestUri = new Uri($"{callbackUrl.GetLeftPart(UriPartial.Authority)}{requestParts[1]}");
        if (!string.Equals(requestUri.AbsolutePath, callbackUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The sign-in callback path was invalid.");
        }

        return requestUri;
    }

    private static string BuildLoopbackBrowserResponse(bool hasError)
    {
        var title = hasError ? "Sign-in failed" : "Sign-in completed";
        var body = $"""
            <!doctype html>
            <html>
            <head><meta charset="utf-8"><title>{title}</title></head>
            <body style="font-family:Segoe UI,Arial,sans-serif;margin:48px;color:#17202A">
            <h1>{title}</h1>
            <p>You can close this browser tab and return to PrivateCloudDrive.</p>
            </body>
            </html>
            """;
        var bodyBytes = Encoding.UTF8.GetByteCount(body);

        return string.Join(
            "\r\n",
            "HTTP/1.1 200 OK",
            "Content-Type: text/html; charset=utf-8",
            "Connection: close",
            $"Content-Length: {bodyBytes}",
            "",
            body);
    }
#endif

    private static Uri BuildAuthorizeUri(string state, string codeChallenge, string redirectUri)
    {
        var query = BuildQueryString(
            new Dictionary<string, string>
            {
                ["client_id"] = AppSettings.OAuthClientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = AppSettings.OAuthScopes,
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256"
            });

        return new Uri($"{AppSettings.ApiBaseUrl.TrimEnd('/')}/connect/authorize?{query}");
    }

    private static Uri BuildExternalAuthorizeUri(
        ExternalLoginProviderSettings provider,
        string state,
        string? codeChallenge,
        string redirectUri)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId!,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["state"] = state
        };

        AddOptionalParameter(parameters, "scope", provider.Scope);

        if (!string.IsNullOrWhiteSpace(codeChallenge))
        {
            parameters["code_challenge"] = codeChallenge;
            parameters["code_challenge_method"] = "S256";
        }

        return new Uri($"{provider.AuthorizationEndpoint}?{BuildQueryString(parameters)}");
    }

    private async Task<TokenResponse> RequestTokenAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(parameters);
        HttpResponseMessage response;
        try
        {
            EnsureBaseAddressCurrent();
            response = await _httpClient.PostAsync("connect/token", content, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Authentication network request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException("Cannot reach authentication server.", exception);
        }

        using (response)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw CreateOAuthTokenException(response.StatusCode, response.ReasonPhrase, responseText);
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseText);
            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("OpenIddict returned an invalid token response.");
            }

            return tokenResponse;
        }
    }

    private async Task RecordAuditAsync(
        string provider,
        string action,
        string result,
        string? failureReason,
        string? userName,
        CancellationToken cancellationToken)
    {
        try
        {
            var input = new MobileAuthAuditLogInput
            {
                Provider = provider,
                Action = action,
                Result = result,
                FailureReason = failureReason,
                ClientId = AppSettings.OAuthClientId,
                UserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim(),
                UserAgent = "PrivateCloudDrive.MAUI"
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(input),
                Encoding.UTF8,
                "application/json");

            EnsureBaseAddressCurrent();
            using var response = await _httpClient.PostAsync(
                "api/mobile-auth/audit-logs",
                content,
                cancellationToken);
            _ = response;
        }
        catch
        {
            // Authentication flow must not fail only because audit reporting is unavailable.
        }
    }

    private void EnsureBaseAddressCurrent()
    {
        var currentBaseAddress = new Uri(AppSettings.ApiBaseUrl.TrimEnd('/') + "/");
        if (_httpClient.BaseAddress != currentBaseAddress)
        {
            _httpClient.BaseAddress = currentBaseAddress;
        }
    }

    private static string? NormalizeAuditReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 512
            ? trimmed
            : trimmed[..512];
    }

    private static async Task SaveTokenSetAsync(
        TokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokenSet = new StoredTokenSet
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenType = tokenResponse.TokenType,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };

        await SecureStorage.Default.SetAsync(TokenStorageKey, JsonSerializer.Serialize(tokenSet));
    }

    private static async Task<StoredTokenSet?> LoadTokenSetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokenJson = await SecureStorage.Default.GetAsync(TokenStorageKey);
        return string.IsNullOrWhiteSpace(tokenJson)
            ? null
            : JsonSerializer.Deserialize<StoredTokenSet>(tokenJson);
    }

    private static string CreateBase64UrlRandom(int byteCount)
    {
        return ToBase64Url(RandomNumberGenerator.GetBytes(byteCount));
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        return ToBase64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string> parameters)
    {
        return string.Join(
            "&",
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

    private static void AddOptionalParameter(
        IDictionary<string, string> parameters,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[name] = value.Trim();
        }
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = parameter.IndexOf('=');
            var key = separatorIndex >= 0 ? parameter[..separatorIndex] : parameter;
            var value = separatorIndex >= 0 ? parameter[(separatorIndex + 1)..] : string.Empty;

            values[DecodeQueryStringValue(key)] = DecodeQueryStringValue(value);
        }

        return values;
    }

    private static string DecodeQueryStringValue(string value)
    {
        return Uri.UnescapeDataString(value.Replace("+", " "));
    }

    private static string GetOAuthError(string responseText)
    {
        return CreateOAuthTokenException(null, null, responseText).Message;
    }

    private static OAuthTokenException CreateOAuthTokenException(
        System.Net.HttpStatusCode? statusCode,
        string? reasonPhrase,
        string responseText)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OAuthErrorResponse>(responseText);
            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                var safeMessage = BuildOAuthErrorMessage(statusCode, reasonPhrase, error.Error);
                return new OAuthTokenException(error.Error, safeMessage, error.BindingTicket);
            }
        }
        catch
        {
            // Fall through to status-based safe classification. Never surface raw OAuth response bodies.
        }

        var message = BuildOAuthErrorMessage(statusCode, reasonPhrase, oauthError: null);
        return new OAuthTokenException("invalid_grant", message, null);
    }

    private static string BuildOAuthErrorMessage(
        System.Net.HttpStatusCode? statusCode,
        string? reasonPhrase,
        string? oauthError)
    {
        if (statusCode.HasValue && (int)statusCode.Value >= 500)
        {
            return $"Authentication server error. HTTP {(int)statusCode.Value}.";
        }

        if (string.Equals(oauthError, WechatBindingRequiredError, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(oauthError, ExternalBindingRequiredError, StringComparison.OrdinalIgnoreCase))
        {
            return "Account binding is required before sign-in can continue.";
        }

        if (string.Equals(oauthError, "invalid_grant", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(oauthError, "invalid_client", StringComparison.OrdinalIgnoreCase) ||
            statusCode == HttpStatusCode.BadRequest ||
            statusCode == HttpStatusCode.Unauthorized ||
            statusCode == HttpStatusCode.Forbidden)
        {
            return "Invalid username or password.";
        }

        var reason = string.IsNullOrWhiteSpace(reasonPhrase)
            ? string.Empty
            : $" {reasonPhrase.Trim()}";

        return statusCode.HasValue
            ? $"Authentication request failed. HTTP {(int)statusCode.Value}{reason}.".Trim()
            : "Authentication request failed.";
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }

    private sealed class StoredTokenSet
    {
        public string AccessToken { get; init; } = string.Empty;

        public string? RefreshToken { get; init; }

        public string TokenType { get; init; } = "Bearer";

        public DateTimeOffset ExpiresAt { get; init; }
    }

    private sealed class OAuthErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }

        [JsonPropertyName("binding_ticket")]
        public string? BindingTicket { get; init; }
    }

    private sealed class OAuthTokenException : InvalidOperationException
    {
        /// <summary>
        /// 执行OAuthTokenException操作，封装该场景下的业务规则、异常处理和结果返回。
        /// </summary>
        public OAuthTokenException(string error, string message, string? bindingTicket)
            : base(message)
        {
            Error = error;
            BindingTicket = bindingTicket;
        }

        public string Error { get; }

        public string? BindingTicket { get; }
    }

    private sealed class MobileAuthAuditLogInput
    {
        public string Provider { get; init; } = string.Empty;

        public string Action { get; init; } = string.Empty;

        public string Result { get; init; } = string.Empty;

        public string? FailureReason { get; init; }

        public string? ClientId { get; init; }

        public string? UserName { get; init; }

        public string? UserAgent { get; init; }
    }
}
