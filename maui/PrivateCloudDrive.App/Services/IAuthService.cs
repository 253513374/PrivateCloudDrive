namespace PrivateCloudDrive.App.Services;

public interface IAuthService
{
    Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default);

    Task SignInAsync(CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);

    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
