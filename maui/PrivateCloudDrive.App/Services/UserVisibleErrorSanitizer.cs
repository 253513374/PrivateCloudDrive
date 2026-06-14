using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using PrivateCloudDrive.App.Localization;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// Keeps raw exception details in local diagnostics while exposing only safe summaries in UI.
/// </summary>
public static partial class UserVisibleErrorSanitizer
{
    public const string GenericSettingsError = "操作未完成。请确认私有服务器、网络和登录状态后重试；为保护隐私，详细错误不在页面中展示。";
    public const string GenericStorageError = "无法读取容量。请确认私有服务器、网络和登录状态后重试；为保护隐私，详细错误不在页面中展示。";
    public const string GenericSystemHealthError = "无法读取系统健康状态。请稍后重试；为保护隐私，详细错误不在页面中展示。";
    public const string GenericBackupError = "备份未完成。请确认私有服务器、网络、容量和登录状态后重试；为保护隐私，详细错误不在队列中展示。";
    public const string GenericSignInError = "登录未完成。请确认账号、网络和私有服务器状态后重试；为保护隐私，详细错误不在页面中展示。";

    public static string ForSettings(Exception exception, string? fallback = null)
    {
        WriteLocalDiagnostic(exception);
        return string.IsNullOrWhiteSpace(fallback) ? GenericSettingsError : fallback;
    }

    public static string ForStorage(Exception exception, string? fallback = null)
    {
        WriteLocalDiagnostic(exception);
        return string.IsNullOrWhiteSpace(fallback) ? GenericStorageError : fallback;
    }

    public static string ForSystemHealth(Exception exception, string? fallback = null)
    {
        WriteLocalDiagnostic(exception);
        return string.IsNullOrWhiteSpace(fallback) ? GenericSystemHealthError : fallback;
    }

    public static string ForBackup(Exception exception)
    {
        WriteLocalDiagnostic(exception);

        if (exception is HttpRequestException or IOException)
        {
            return "无法连接到私有服务器。请确认后端服务和网络恢复后，点击“重试备份”。";
        }

        if (exception is TaskCanceledException or TimeoutException)
        {
            return "备份请求超时。请确认网络稳定或服务器恢复后，点击“重试备份”。";
        }

        return GenericBackupError;
    }

    public static string ForSignIn(Exception exception)
    {
        WriteLocalDiagnostic(exception);

        if (IsRecoverableServerConnectionError(exception))
        {
            return AppText.SignInServerUnavailable;
        }

        if (IsInvalidCredentialError(exception))
        {
            return AppText.InvalidUserNameOrPassword;
        }

        return GenericSignInError;
    }

    private static bool IsInvalidCredentialError(Exception exception)
    {
        if (exception is OAuthTokenException tokenException)
        {
            return IsCredentialStatusCode(tokenException.StatusCode) &&
                (tokenException.Error.Equals("invalid_grant", StringComparison.OrdinalIgnoreCase) ||
                 tokenException.Message.Contains("Invalid username or password", StringComparison.OrdinalIgnoreCase));
        }

        return exception.Message.Contains("Invalid username or password", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecoverableServerConnectionError(Exception exception)
    {
        if (exception is OAuthTokenException tokenException)
        {
            return IsRecoverableServerStatusCode(tokenException.StatusCode);
        }

        if (exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return true;
        }

        return exception.InnerException != null && IsRecoverableServerConnectionError(exception.InnerException);
    }

    private static bool IsCredentialStatusCode(HttpStatusCode? statusCode)
    {
        return statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized;
    }

    private static bool IsRecoverableServerStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode == null)
        {
            return false;
        }

        var numericStatusCode = (int)statusCode.Value;
        return numericStatusCode >= 500 ||
            statusCode is HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout or
                HttpStatusCode.RequestTimeout;
    }

    public static string RedactUserVisibleText(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var redacted = UrlRegex().Replace(value, "[已隐藏地址]");
        redacted = TokenLikeRegex().Replace(redacted, "$1=[已隐藏]");
        redacted = WindowsPathRegex().Replace(redacted, "[已隐藏路径]");
        redacted = UnixPrivatePathRegex().Replace(redacted, "[已隐藏路径]");

        return string.IsNullOrWhiteSpace(redacted) ? fallback : redacted;
    }

    public static string SafeServerLabel(bool hasCustomApiBaseUrl)
    {
        return hasCustomApiBaseUrl
            ? "自定义私有服务器 · 完整地址已隐藏"
            : "默认私有服务器 · 完整地址已隐藏";
    }

    private static void WriteLocalDiagnostic(Exception exception)
    {
        Debug.WriteLine($"[PrivateCloudDrive] Local diagnostic: {exception}");
    }

    [GeneratedRegex(@"https?://[^\s　；，,。)）>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"(token|cookie|secret|password|passwd|accesskey|access_key|connectionstring|connection_string)\s*[:=]\s*[^\s　；，,。]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TokenLikeRegex();

    [GeneratedRegex(@"[A-Za-z]:\[^\s　；，,。]+", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsPathRegex();

    [GeneratedRegex(@"/(?:home|var|etc|mnt|data|app|tmp|opt|usr)/[^\s　；，,。]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnixPrivatePathRegex();
}
