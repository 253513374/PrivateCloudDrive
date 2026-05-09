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

    Task RestoreTrashItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task PermanentlyDeleteTrashItemAsync(
        Guid id,
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

    Task<CloudDriveShare> CreateShareAsync(
        Guid itemId,
        DateTime? expirationTime,
        bool allowDownload,
        string? password,
        CancellationToken cancellationToken = default);

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
