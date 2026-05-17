using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 提供UploadQueue服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public sealed class UploadQueueService : IUploadQueueService
{
    public ObservableCollection<UploadQueueItem> Items { get; } = [];

    /// <summary>
    /// 执行Enqueue操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public UploadQueueItem Enqueue(FileResult file, string targetPath, Guid? targetFolderId)
    {
        var item = new UploadQueueItem(file, targetPath, targetFolderId);
        Items.Insert(0, item);
        return item;
    }

    /// <summary>
    /// 重置指定对象的临时安全状态或缓存状态。
    /// </summary>
    public void ClearCompleted()
    {
        foreach (var item in Items.Where(item => item.IsCompleted).ToList())
        {
            Items.Remove(item);
        }
    }
}
