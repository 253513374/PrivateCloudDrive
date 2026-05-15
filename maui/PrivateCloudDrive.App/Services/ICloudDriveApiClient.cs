using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 定义CloudDriveApiClient抽象契约，用于解耦调用方与具体实现。
/// </summary>
public interface ICloudDriveApiClient
{
    Task<IReadOnlyList<CloudDriveItem>> GetItemsAsync(
        Guid? parentId,
        int skipCount = 0,
        int maxResultCount = 50,
        CloudDriveQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveItem>> GetTrashItemsAsync(
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default);

    Task<CloudDriveItem> CreateFolderAsync(
        Guid? parentId,
        string name,
        CancellationToken cancellationToken = default);

    Task DeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task DeleteItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task RestoreTrashItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveItem>> RestoreTrashItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task PermanentlyDeleteTrashItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task PermanentlyDeleteTrashItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task EmptyTrashAsync(CancellationToken cancellationToken = default);

    Task UploadFileAsync(
        Guid? parentId,
        FileResult file,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<FileContentResult> GetFileContentAsync(
        Guid id,
        bool thumbnail,
        CancellationToken cancellationToken = default);

    Task<RemoteFileContentSource> GetRemoteFileContentSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<string> DownloadFileToCacheAsync(
        Guid id,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveTag>> GetTagsAsync(CancellationToken cancellationToken = default);

    Task<CloudDriveTag> CreateTagAsync(
        string name,
        string? color,
        CancellationToken cancellationToken = default);

    Task AddTagToItemAsync(
        Guid itemId,
        Guid tagId,
        CancellationToken cancellationToken = default);

    Task<CloudDriveItem> SetFavoriteAsync(
        Guid itemId,
        bool isFavorite,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveItem>> SetFavoriteItemsAsync(
        IReadOnlyCollection<Guid> ids,
        bool isFavorite,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveItem>> MoveItemsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? parentId,
        CancellationToken cancellationToken = default);

    Task<CloudDriveShare> CreateShareAsync(
        Guid itemId,
        DateTime? expirationTime,
        bool allowDownload,
        string? password,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveShare>> GetSharesAsync(
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default);

    Task DisableShareAsync(
        Guid shareId,
        CancellationToken cancellationToken = default);

    Task<StorageUsage> GetStorageUsageAsync(CancellationToken cancellationToken = default);

    Task<SystemHealthSummary> GetSystemHealthSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudOperationLog>> GetOperationLogsAsync(
        int skipCount = 0,
        int maxResultCount = 30,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveItem>> GetImagesAsync(
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudDriveItem>> GetVideosAsync(
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaTimelineItem>> GetMediaTimelineAsync(
        string? mediaType = null,
        Guid? albumId = null,
        string? processStatus = null,
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default);

    Task<MediaDetail> GetMediaDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaTimelineItem>> GetMediaProcessingItemsAsync(
        string? status = null,
        string? mediaType = null,
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default);

    Task<MediaDetail> RetryMediaProcessingAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaAlbum>> GetMediaAlbumsAsync(
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default);

    Task<MediaAlbum> CreateMediaAlbumAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<MediaAlbum> UpdateMediaAlbumAsync(
        Guid id,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task DeleteMediaAlbumAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaTimelineItem>> GetMediaAlbumItemsAsync(
        Guid id,
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaTimelineItem>> AddMediaAlbumItemsAsync(
        Guid id,
        IReadOnlyCollection<Guid> fileNodeIds,
        CancellationToken cancellationToken = default);

    Task RemoveMediaAlbumItemAsync(
        Guid id,
        Guid fileNodeId,
        CancellationToken cancellationToken = default);

    Task<MediaAlbum> SetMediaAlbumCoverAsync(
        Guid id,
        Guid fileNodeId,
        CancellationToken cancellationToken = default);

    Task<WechatLoginSettings> GetWechatLoginSettingsAsync(CancellationToken cancellationToken = default);

    Task<WechatBinding?> GetWechatBindingAsync(CancellationToken cancellationToken = default);

    Task<WechatBinding> BindCurrentWechatAsync(
        string code,
        string? state,
        string? platform,
        string? deviceIdHash,
        CancellationToken cancellationToken = default);

    Task<WechatBinding> BindExistingWechatAsync(
        string bindingTicket,
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default);

    Task UnbindWechatAsync(CancellationToken cancellationToken = default);

    Task<ExternalLoginSettings> GetExternalLoginSettingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalBinding>> GetExternalBindingsAsync(CancellationToken cancellationToken = default);

    Task<ExternalBinding> BindCurrentExternalAsync(
        string provider,
        string code,
        string? state,
        string redirectUri,
        string? codeVerifier,
        string? deviceIdHash,
        CancellationToken cancellationToken = default);

    Task<ExternalBinding> BindExistingExternalAsync(
        string bindingTicket,
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default);

    Task UnbindExternalAsync(
        string provider,
        CancellationToken cancellationToken = default);
}
