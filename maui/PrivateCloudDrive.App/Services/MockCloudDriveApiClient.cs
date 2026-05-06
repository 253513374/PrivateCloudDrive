using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public sealed class MockCloudDriveApiClient
{
    private static bool _isSignedIn;

    public bool IsSignedIn => _isSignedIn;

    public Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        _isSignedIn = !string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password);
        return Task.FromResult(_isSignedIn);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _isSignedIn = false;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CloudDriveItem>> GetRootItemsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CloudDriveItem> items =
        [
            new("Photos", "Folder", "12 items", "Today", "DIR"),
            new("Videos", "Folder", "5 items", "Yesterday", "DIR"),
            new("Contracts.pdf", "PDF", "1.8 MB", "May 4", "PDF"),
            new("Family-trip.jpg", "Image", "4.2 MB", "May 2", "IMG"),
            new("Backup.zip", "Archive", "860 MB", "Apr 28", "ZIP")
        ];

        return Task.FromResult(items);
    }
}
