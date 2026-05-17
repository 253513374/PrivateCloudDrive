using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// Coordinates file selection results, upload queue state, API upload execution, and retry diagnostics.
/// </summary>
public sealed class BackupTransferService : IBackupTransferService
{
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
            await _apiClient.UploadFileAsync(item.TargetFolderId, item.File, progress, cancellationToken);
            item.MarkCompleted();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            item.MarkFailed(await WriteBackupErrorAsync(exception));
        }
    }

    private static async Task<string> WriteBackupErrorAsync(Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? AppText.Format(nameof(AppText.UploadFailedBeforeRequest), exception.GetType().Name)
            : exception.Message;

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
}
