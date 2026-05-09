using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 提供IUploadQueue服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public interface IUploadQueueService
{
    ObservableCollection<UploadQueueItem> Items { get; }

    UploadQueueItem Enqueue(FileResult file, string targetPath);

    void ClearCompleted();
}
