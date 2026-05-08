using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public interface IAuthService
{
    Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default);

    Task SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    Task<WechatSignInResult> SignInWithWechatCodeAsync(
        string code,
        string? state,
        string? platform,
        string? deviceIdHash,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);

    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
