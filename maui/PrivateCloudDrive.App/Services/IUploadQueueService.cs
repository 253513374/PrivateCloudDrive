using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public interface IUploadQueueService
{
    ObservableCollection<UploadQueueItem> Items { get; }

    UploadQueueItem Enqueue(FileResult file, string targetPath);

    void ClearCompleted();
}
