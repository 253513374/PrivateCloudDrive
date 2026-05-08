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

public sealed class OpenIddictAuthService : IAuthService
{
    private const string TokenStorageKey = "auth.tokens";
    private const string WechatGrantType = "urn:privateclouddrive:wechat";
    private const string WechatBindingRequiredError = "wechat_binding_required";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(AppSettings.ApiBaseUrl)
    };

    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default)
    {
        return !string.IsNullOrWhiteSpace(await GetAccessTokenAsync(cancellationToken));
    }

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

    private static async Task<IReadOnlyDictionary<string, string>> AuthenticateAsync(
        Uri authorizationUrl,
        Uri callbackUrl,
        CancellationToken cancellationToken)
    {
#if WINDOWS
        return await AuthenticateWithLoopbackRedirectAsync(authorizationUrl, callbackUrl, cancellationToken);
#else
        var result = await WebAuthenticator.Default.AuthenticateAsync(authorizationUrl, callbackUrl);
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

    private async Task<TokenResponse> RequestTokenAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(parameters);
        using var response = await _httpClient.PostAsync("connect/token", content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateOAuthTokenException(responseText);
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseText);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("OpenIddict returned an invalid token response.");
        }

        return tokenResponse;
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
        return CreateOAuthTokenException(responseText).Message;
    }

    private static OAuthTokenException CreateOAuthTokenException(string responseText)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OAuthErrorResponse>(responseText);
            if (!string.IsNullOrWhiteSpace(error?.ErrorDescription))
            {
                return new OAuthTokenException(
                    error.Error ?? "invalid_grant",
                    error.ErrorDescription,
                    error.BindingTicket);
            }

            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                return new OAuthTokenException(error.Error, error.Error, error.BindingTicket);
            }
        }
        catch
        {
            // Fall through to the raw response body.
        }

        var message = string.IsNullOrWhiteSpace(responseText)
            ? "OpenIddict token request failed."
            : responseText;
        return new OAuthTokenException("invalid_grant", message, null);
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
