using System.Net;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// Represents a failed OAuth token endpoint response with enough context for UI error classification.
/// </summary>
public sealed class OAuthTokenException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenException"/> class.
    /// </summary>
    public OAuthTokenException(
        string error,
        string message,
        string? bindingTicket,
        HttpStatusCode? statusCode)
        : base(message)
    {
        Error = error;
        BindingTicket = bindingTicket;
        StatusCode = statusCode;
    }

    public string Error { get; }

    public string? BindingTicket { get; }

    public HttpStatusCode? StatusCode { get; }
}
