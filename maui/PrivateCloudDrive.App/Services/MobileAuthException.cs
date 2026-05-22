using System.Net;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 移动端认证错误分类，用于把底层 HTTP/OAuth/网络异常转换为安全、可本地化的用户提示。
/// </summary>
public enum MobileAuthErrorKind
{
    Unknown,
    ServiceUnavailable,
    NetworkError,
    InvalidCredentials,
    ServerError
}

/// <summary>
/// 表示可向登录页安全分类展示的认证异常；Message 仅用于日志和审计，UI 必须根据 Kind 显示固定文案。
/// </summary>
public class MobileAuthException : InvalidOperationException
{
    public MobileAuthException(
        MobileAuthErrorKind kind,
        string message,
        Exception? innerException = null,
        HttpStatusCode? statusCode = null)
        : base(message, innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
    }

    public MobileAuthErrorKind Kind { get; }

    public HttpStatusCode? StatusCode { get; }
}
