using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// Coordinates user-visible private backup transfers and retry operations.
/// </summary>
public interface IBackupTransferService
{
    Task<IReadOnlyList<UploadQueueItem>> BackupFilesAsync(
        Guid? targetFolderId,
        string targetPath,
        IReadOnlyList<FileResult> files,
        CancellationToken cancellationToken = default);

    Task RetryAsync(UploadQueueItem item, CancellationToken cancellationToken = default);
}
