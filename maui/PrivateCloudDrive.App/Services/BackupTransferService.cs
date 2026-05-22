using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// Coordinates file selection results, upload queue state, API upload execution, and retry diagnostics.
/// </summary>
public sealed class BackupTransferService : IBackupTransferService
{
    private static readonly TimeSpan UploadAttemptTimeout = TimeSpan.FromSeconds(15);

    private readonly ICloudDriveApiClient _apiClient;
    private readonly IUploadQueueService _uploadQueueService;

    public BackupTransferService(
        ICloudDriveApiClient apiClient,
        IUploadQueueService uploadQueueService)
    {
        _apiClient = apiClient;
        _uploadQueueService = uploadQueueService;
    }

    public async Task<IReadOnlyList<UploadQueueItem>> BackupFilesAsync(
        Guid? targetFolderId,
        string targetPath,
        IReadOnlyList<FileResult> files,
        CancellationToken cancellationToken = default)
    {
        var queueItems = files
            .Select(file => _uploadQueueService.Enqueue(file, targetPath, targetFolderId))
            .ToList();

        foreach (var item in queueItems)
        {
            await UploadQueueItemAsync(item, cancellationToken);
        }

        return queueItems;
    }

    public Task RetryAsync(UploadQueueItem item, CancellationToken cancellationToken = default)
    {
        if (!item.CanRetry)
        {
            return Task.CompletedTask;
        }

        return UploadQueueItemAsync(item, cancellationToken);
    }

    private async Task UploadQueueItemAsync(UploadQueueItem item, CancellationToken cancellationToken)
    {
        item.MarkUploading();

        var progress = new Progress<double>(item.UpdateProgress);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(UploadAttemptTimeout);

            await _apiClient.UploadFileAsync(item.TargetFolderId, item.File, progress, timeoutCts.Token);
            item.MarkCompleted();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            item.MarkFailed(await WriteBackupErrorAsync(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not AuthSessionExpiredException)
        {
            item.MarkFailed(await WriteBackupErrorAsync(exception));
        }
    }

    private static async Task<string> WriteBackupErrorAsync(Exception exception)
    {
        var message = GetUserFacingBackupError(exception);

        try
        {
            var logPath = Path.Combine(FileSystem.AppDataDirectory, "backup-errors.log");
            await File.AppendAllTextAsync(
                logPath,
                $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // The UI message is more important than diagnostic logging.
        }

        return message;
    }

    private static string GetUserFacingBackupError(Exception exception)
    {
        return UserVisibleErrorSanitizer.ForBackup(exception);
    }
}
