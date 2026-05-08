using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public sealed class UploadQueueService : IUploadQueueService
{
    public ObservableCollection<UploadQueueItem> Items { get; } = [];

    public UploadQueueItem Enqueue(FileResult file, string targetPath)
    {
        var item = new UploadQueueItem(file, targetPath);
        Items.Insert(0, item);
        return item;
    }

    public void ClearCompleted()
    {
        foreach (var item in Items.Where(item => item.IsCompleted).ToList())
        {
            Items.Remove(item);
        }
    }
}
