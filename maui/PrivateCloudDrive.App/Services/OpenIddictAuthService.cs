using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;

namespace PrivateCloudDrive.App.Services;

public sealed class OpenIddictAuthService : IAuthService
{
    private const string TokenStorageKey = "auth.tokens";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(AppSettings.ApiBaseUrl)
    };

    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default)
    {
        return !string.IsNullOrWhiteSpace(await GetAccessTokenAsync(cancellationToken));
    }

    public async Task SignInAsync(CancellationToken cancellationToken = default)
    {
        var state = CreateBase64UrlRandom(32);
        var codeVerifier = CreateBase64UrlRandom(32);
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var callbackUrl = new Uri(AppSettings.OAuthRedirectUri);
        var authorizationUrl = BuildAuthorizeUri(state, codeChallenge);

        var result = await WebAuthenticator.Default.AuthenticateAsync(authorizationUrl, callbackUrl);
        cancellationToken.ThrowIfCancellationRequested();

        if (result.Properties.TryGetValue("error", out var error))
        {
            throw new InvalidOperationException(error);
        }

        if (!result.Properties.TryGetValue("state", out var returnedState) ||
            returnedState != state)
        {
            throw new InvalidOperationException("Invalid OpenIddict sign-in state.");
        }

        if (!result.Properties.TryGetValue("code", out var authorizationCode) ||
            string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new InvalidOperationException("OpenIddict did not return an authorization code.");
        }

        var tokenResponse = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = AppSettings.OAuthClientId,
                ["redirect_uri"] = AppSettings.OAuthRedirectUri,
                ["code"] = authorizationCode,
                ["code_verifier"] = codeVerifier
            },
            cancellationToken);

        await SaveTokenSetAsync(tokenResponse, cancellationToken);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        SecureStorage.Default.Remove(TokenStorageKey);
        return Task.CompletedTask;
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
        catch
        {
            await SignOutAsync(cancellationToken);
            return null;
        }
    }

    private static Uri BuildAuthorizeUri(string state, string codeChallenge)
    {
        var query = BuildQueryString(
            new Dictionary<string, string>
            {
                ["client_id"] = AppSettings.OAuthClientId,
                ["redirect_uri"] = AppSettings.OAuthRedirectUri,
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
            throw new InvalidOperationException(GetOAuthError(responseText));
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseText);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("OpenIddict returned an invalid token response.");
        }

        return tokenResponse;
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

    private static string GetOAuthError(string responseText)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OAuthErrorResponse>(responseText);
            if (!string.IsNullOrWhiteSpace(error?.ErrorDescription))
            {
                return error.ErrorDescription;
            }

            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                return error.Error;
            }
        }
        catch
        {
            // Fall through to the raw response body.
        }

        return string.IsNullOrWhiteSpace(responseText)
            ? "OpenIddict token request failed."
            : responseText;
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
    }
}
