namespace PrivateCloudDrive.App.Models;

/// <summary>
/// MAUI 调起外部浏览器授权后解析出的授权结果。
/// CodeVerifier 仅用于后续 token 交换，不应持久化。
/// </summary>
public sealed record ExternalAuthorizationResult(
    string Provider,
    string Code,
    string? State,
    string RedirectUri,
    string? CodeVerifier);
