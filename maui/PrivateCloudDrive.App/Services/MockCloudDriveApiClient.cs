using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示MockCloudDriveApiClient组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed class MockCloudDriveApiClient
{
    private sealed record MockUploadSession(
        string StatusReason,
        string NextAction,
        string? FailureReason,
        int UploadedChunkCount,
        long UploadedBytes,
        decimal ProgressPercent,
        bool IsRetryable);

    private static readonly MockUploadSession[] MockUploadSessions =
    [
        new("WaitingForChunks", "UploadMissingChunks", null, 2, 16 * 1024 * 1024, 25, true),
        new("Completed", "OpenFile", null, 8, 64 * 1024 * 1024, 100, false),
        new("Cancelled", "StartNewUploadSession", "PrivateCloudDrive:FileCenter:000033", 0, 0, 0, false),
        new("ServerVendorPaused", "VendorSpecificAction", "UnknownVendorReason", 1, 4 * 1024 * 1024, 8, true)
    ];

    private static bool _isSignedIn;

    public bool IsSignedIn => _isSignedIn;

    /// <summary>
    /// 执行登录流程，统一处理身份校验、绑定状态、安全审计和错误返回。
    /// </summary>
    public Task<bool> SignInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        _isSignedIn = !string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password);
        return Task.FromResult(_isSignedIn);
    }

    /// <summary>
    /// 执行SignOut操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _isSignedIn = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public Task<IReadOnlyList<CloudDriveItem>> GetItemsAsync(
        Guid? parentId,
        int skipCount = 0,
        int maxResultCount = 50,
        CloudDriveQueryOptions? options = null,
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

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public Task<(IReadOnlyList<CloudDriveItem> Items, long TotalCount)> SearchItemsAsync(
        string keyword,
        string? searchScope = null,
        string? nodeType = null,
        string? mediaType = null,
        string? sorting = null,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        var filtered = GetFilteredMockItems(keyword, nodeType);
        var total = filtered.Count;
        var page = filtered.Skip(skipCount).Take(maxResultCount).ToList();
        return Task.FromResult<(IReadOnlyList<CloudDriveItem>, long)>((page, total));
    }

    private static IReadOnlyList<CloudDriveItem> GetFilteredMockItems(string? keyword, string? nodeType)
    {
        var all = new List<CloudDriveItem>
        {
            new(Guid.NewGuid(), null, "Photos", "Folder", "12 items", "Today", "DIR", null),
            new(Guid.NewGuid(), null, "Videos", "Folder", "5 items", "Yesterday", "DIR", null),
            new(Guid.NewGuid(), null, "Contracts.pdf", "PDF", "1.8 MB", "May 4", "PDF", "application/pdf"),
            new(Guid.NewGuid(), null, "Family-trip.jpg", "Image", "4.2 MB", "May 2", "IMG", "image/jpeg"),
            new(Guid.NewGuid(), null, "Backup.zip", "Archive", "860 MB", "Apr 28", "ZIP", "application/zip")
        };

        var items = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            items = items.Where(i => i.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (nodeType is "Folder")
        {
            items = items.Where(i => i.IsFolder);
        }
        else if (nodeType is "File")
        {
            items = items.Where(i => !i.IsFolder);
        }

        return items.ToList();
    }
}
