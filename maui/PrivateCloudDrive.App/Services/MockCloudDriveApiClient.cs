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

    public Task<IReadOnlyList<CloudDriveItem>> GetItemsAsync(
        Guid? parentId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CloudDriveItem> items =
        [
            new(Guid.NewGuid(), parentId, "Photos", "Folder", "12 items", "Today", "DIR", null),
            new(Guid.NewGuid(), parentId, "Videos", "Folder", "5 items", "Yesterday", "DIR", null),
            new(Guid.NewGuid(), parentId, "Contracts.pdf", "PDF", "1.8 MB", "May 4", "PDF", "application/pdf"),
            new(Guid.NewGuid(), parentId, "Family-trip.jpg", "Image", "4.2 MB", "May 2", "IMG", "image/jpeg"),
            new(Guid.NewGuid(), parentId, "Backup.zip", "Archive", "860 MB", "Apr 28", "ZIP", "application/zip")
        ];

        return Task.FromResult(items);
    }
}
