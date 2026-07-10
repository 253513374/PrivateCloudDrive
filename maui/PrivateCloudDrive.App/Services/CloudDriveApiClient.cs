using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// MAUI 客户端访问 PrivateCloudDrive 后端文件中心 API 的实现。
/// 负责自动附加 Bearer Token、解析 ABP 响应，并按文件大小选择小文件或分片上传策略。
/// </summary>
public sealed class CloudDriveApiClient : ICloudDriveApiClient
{
    private const int ChunkSize = 8 * 1024 * 1024;
    private const long SmallUploadThreshold = 32L * 1024 * 1024;

    private readonly IAuthService _authService;
    private readonly HttpClient _httpClient = new(CreateHttpClientHandler())
    {
        BaseAddress = new Uri(AppSettings.ApiBaseUrl)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 初始化 <see cref="CloudDriveApiClient"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public CloudDriveApiClient(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 获取指定目录下的文件和文件夹列表。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveItem>> GetItemsAsync(
        Guid? parentId,
        int skipCount = 0,
        int maxResultCount = 50,
        CloudDriveQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = BuildFolderListPath(parentId, skipCount, maxResultCount, options);

        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            path,
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<FileNodeDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToCloudDriveItem).ToList() ?? [];
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveItem>> GetTrashItemsAsync(
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/file-center/trash?SkipCount={skipCount}&MaxResultCount={maxResultCount}";
        using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, path, cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<FileNodeDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToCloudDriveItem).ToList() ?? [];
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public async Task<CloudDriveItem> CreateFolderAsync(
        Guid? parentId,
        string name,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/app/file-center-folders",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new CreateFolderRequest
            {
                ParentId = parentId,
                Name = name
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var created = JsonSerializer.Deserialize<FileNodeDto>(responseText, JsonOptions)
                      ?? throw new InvalidOperationException("Create folder response is invalid.");

        return ToCloudDriveItem(created);
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            $"/api/file-center/nodes/{id}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 批量删除文件或文件夹。
    /// </summary>
    public Task DeleteItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return SendBatchNoContentAsync(
            "/api/file-center/nodes/batch/delete",
            new BatchFileNodeRequest { Ids = ids.ToList() },
            cancellationToken);
    }

    /// <summary>
    /// 从回收站或临时状态恢复资源，并校验恢复位置和命名冲突。
    /// </summary>
    public async Task RestoreTrashItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/nodes/{id}/restore",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 批量恢复回收站资源。
    /// </summary>
    public Task<IReadOnlyList<CloudDriveItem>> RestoreTrashItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return SendBatchItemsAsync(
            "/api/file-center/nodes/batch/restore",
            new BatchFileNodeRequest { Ids = ids.ToList() },
            cancellationToken);
    }

    /// <summary>
    /// 执行PermanentlyDeleteTrashItem操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public async Task PermanentlyDeleteTrashItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            $"/api/file-center/nodes/{id}/permanent",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 批量永久删除回收站资源。
    /// </summary>
    public Task PermanentlyDeleteTrashItemsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return SendBatchNoContentAsync(
            "/api/file-center/nodes/batch/permanent-delete",
            new BatchFileNodeRequest { Ids = ids.ToList() },
            cancellationToken);
    }

    /// <summary>
    /// 执行EmptyTrash操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public async Task EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            "/api/file-center/trash",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 上传本地文件；小文件直接上传，大文件按固定分片大小走上传会话。
    /// </summary>
    public async Task UploadFileAsync(
        Guid? parentId,
        FileResult file,
        IProgress<UploadTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = await file.OpenReadAsync();
        var fileSize = stream.CanSeek ? stream.Length : 0;

        progress?.Report(new UploadTransferProgress(0, totalBytes: fileSize, statusReason: "WaitingForChunks", nextAction: "UploadMissingChunks"));

        if (!stream.CanSeek || fileSize <= SmallUploadThreshold)
        {
            await UploadSmallFileAsync(parentId, file, stream, progress, cancellationToken);
            progress?.Report(new UploadTransferProgress(1, totalBytes: fileSize, uploadedBytes: fileSize, progressPercent: 100, statusReason: "Completed", nextAction: "OpenFile"));
            return;
        }

        await UploadChunkedFileAsync(parentId, file, stream, fileSize, progress, cancellationToken);
        progress?.Report(new UploadTransferProgress(1, totalBytes: fileSize, uploadedBytes: fileSize, progressPercent: 100, statusReason: "Completed", nextAction: "OpenFile"));
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<FileContentResult> GetFileContentAsync(
        Guid id,
        bool thumbnail,
        CancellationToken cancellationToken = default)
    {
        var path = thumbnail
            ? $"/api/file-center/files/{id}/thumbnail"
            : $"/api/file-center/files/{id}/content";

        using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, path, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseText = Encoding.UTF8.GetString(bytes);
            EnsureSuccess(response, responseText);
        }

        return new FileContentResult
        {
            Content = bytes,
            ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"
        };
    }

    /// <summary>
    /// 为媒体预览组件生成远程文件流地址和授权头，避免把文件完整下载到内存后再播放。
    /// </summary>
    public async Task<RemoteFileContentSource> GetRemoteFileContentSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AuthSessionExpiredException(AppText.SignInRequired);
        }

        return new RemoteFileContentSource(
            new Uri(_httpClient.BaseAddress!, $"/api/file-center/files/{id}/content"),
            new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {accessToken}"
            });
    }

    /// <summary>
    /// 使用应用自己的授权请求下载文件到本地缓存，供不支持自定义请求头的系统播放器读取。
    /// </summary>
    public async Task<string> DownloadFileToCacheAsync(
        Guid id,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"/api/file-center/files/{id}/content",
            cancellationToken);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, responseText);
        }

        var localPath = BuildCacheFilePath(
            id,
            fileName,
            response.Content.Headers.ContentType?.MediaType);

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(localPath);
        await source.CopyToAsync(target, cancellationToken);

        return localPath;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveTag>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/tags",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var tags = JsonSerializer.Deserialize<List<FileTagDto>>(responseText, JsonOptions) ?? [];
        return tags
            .Select(tag => new CloudDriveTag(tag.Id, tag.Name, tag.Color))
            .ToList();
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public async Task<CloudDriveTag> CreateTagAsync(
        string name,
        string? color,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/file-center/tags",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new CreateTagRequest
            {
                Name = name,
                Color = color
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var tag = JsonSerializer.Deserialize<FileTagDto>(responseText, JsonOptions)
                  ?? throw new InvalidOperationException("Create tag response is invalid.");

        return new CloudDriveTag(tag.Id, tag.Name, tag.Color);
    }

    /// <summary>
    /// 执行AddTagToItem操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public async Task AddTagToItemAsync(
        Guid itemId,
        Guid tagId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/nodes/{itemId}/tags/{tagId}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 更新现有业务资源，并保持跨层数据和领域状态一致。
    /// </summary>
    public async Task<CloudDriveItem> SetFavoriteAsync(
        Guid itemId,
        bool isFavorite,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/nodes/{itemId}/favorite",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new SetFavoriteRequest
            {
                IsFavorite = isFavorite
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var node = JsonSerializer.Deserialize<FileNodeDto>(responseText, JsonOptions)
                   ?? throw new InvalidOperationException("Set favorite response is invalid.");

        return ToCloudDriveItem(node);
    }

    /// <summary>
    /// 批量设置收藏状态。
    /// </summary>
    public Task<IReadOnlyList<CloudDriveItem>> SetFavoriteItemsAsync(
        IReadOnlyCollection<Guid> ids,
        bool isFavorite,
        CancellationToken cancellationToken = default)
    {
        return SendBatchItemsAsync(
            "/api/file-center/nodes/batch/favorite",
            new BatchSetFavoriteRequest
            {
                Ids = ids.ToList(),
                IsFavorite = isFavorite
            },
            cancellationToken);
    }

    /// <summary>
    /// 批量移动文件或文件夹。
    /// </summary>
    public Task<IReadOnlyList<CloudDriveItem>> MoveItemsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? parentId,
        CancellationToken cancellationToken = default)
    {
        return SendBatchItemsAsync(
            "/api/file-center/nodes/batch/move",
            new BatchMoveFileNodesRequest
            {
                Ids = ids.ToList(),
                ParentId = parentId
            },
            cancellationToken);
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public async Task<CloudDriveShare> CreateShareAsync(
        Guid itemId,
        DateTime? expirationTime,
        bool allowDownload,
        string? password,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/file-center/shares",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new CreateShareRequest
            {
                FileNodeId = itemId,
                ExpirationTime = expirationTime,
                AllowDownload = allowDownload,
                Password = password
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var share = JsonSerializer.Deserialize<FileShareDto>(responseText, JsonOptions)
                    ?? throw new InvalidOperationException("Create share response is invalid.");

        return new CloudDriveShare(
            share.Id,
            share.FileNodeId,
            share.FileName,
            share.Token,
            share.ExpirationTime,
            share.CreationTime,
            share.AllowDownload,
            share.RequiresPassword,
            share.VisitCount,
            share.IsEnabled,
            share.IsExpired);
    }

    /// <summary>
    /// 获取当前用户的分享管理列表。
    /// </summary>
    public async Task<IReadOnlyList<CloudDriveShare>> GetSharesAsync(
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"/api/file-center/shares?SkipCount={skipCount}&MaxResultCount={maxResultCount}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<FileShareDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToCloudDriveShare).ToList() ?? [];
    }

    /// <summary>
    /// 禁用当前用户拥有的分享链接。
    /// </summary>
    public async Task DisableShareAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            $"/api/file-center/shares/{shareId}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 获取当前用户容量使用摘要。
    /// </summary>
    public async Task<StorageUsage> GetStorageUsageAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/storage/usage",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var usage = JsonSerializer.Deserialize<StorageUsageDto>(responseText, JsonOptions)
                    ?? throw new InvalidOperationException("Storage usage response is invalid.");

        return new StorageUsage(
            usage.UsedBytes,
            usage.QuotaBytes,
            usage.RemainingBytes,
            usage.UsagePercent,
            usage.IsQuotaConfigured,
            usage.MaxSingleFileSize);
    }

    /// <summary>
    /// 获取文件中心系统健康摘要。
    /// </summary>
    public async Task<SystemHealthSummary> GetSystemHealthSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/system-health/summary",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var health = JsonSerializer.Deserialize<SystemHealthDto>(responseText, JsonOptions)
                     ?? throw new InvalidOperationException("System health response is invalid.");

        return new SystemHealthSummary(
            health.OverallStatus,
            health.ApiStatus,
            health.DatabaseStatus,
            health.RedisStatus,
            health.StorageStatus,
            health.FfmpegStatus,
            health.FfprobeStatus,
            health.StorageProvider,
            health.StorageLocationDescription,
            health.BackupScopeDescription,
            health.PrivacyBoundaryDescription,
            health.StorageUsedBytes,
            health.StorageQuotaBytes,
            health.StorageDiskAvailableBytes,
            health.StorageDiskTotalBytes,
            health.IsQuotaConfigured,
            health.GeneratedAt,
            health.Diagnostics);
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<IReadOnlyList<CloudOperationLog>> GetOperationLogsAsync(
        int skipCount = 0,
        int maxResultCount = 30,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"/api/operation-logs?SkipCount={skipCount}&MaxResultCount={maxResultCount}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<OperationLogDto>>(responseText, JsonOptions);
        return result?.Items
            .Select(item => new CloudOperationLog(
                item.Id,
                item.Time,
                item.Source,
                item.Action,
                item.Result,
                item.UserName,
                item.ClientIpAddress,
                item.HttpStatusCode,
                item.Summary))
            .ToList() ?? [];
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public Task<IReadOnlyList<CloudDriveItem>> GetImagesAsync(
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default)
    {
        return GetMediaItemsAsync("/api/file-center/media/images", skipCount, maxResultCount, cancellationToken);
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public Task<IReadOnlyList<CloudDriveItem>> GetVideosAsync(
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default)
    {
        return GetMediaItemsAsync("/api/file-center/media/videos", skipCount, maxResultCount, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaTimelineItem>> GetMediaTimelineAsync(
        string? mediaType = null,
        Guid? albumId = null,
        string? processStatus = null,
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default)
    {
        var query = BuildMediaTimelineQuery(skipCount, maxResultCount, mediaType, albumId, processStatus);
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/media/timeline?" + query,
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<MediaTimelineItemDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToMediaTimelineItem).ToList() ?? [];
    }

    public async Task<MediaDetail> GetMediaDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"/api/file-center/media/{id}/detail",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var detail = JsonSerializer.Deserialize<MediaDetailDto>(responseText, JsonOptions)
                     ?? throw new InvalidOperationException("Media detail response is invalid.");
        return ToMediaDetail(detail);
    }

    public async Task<IReadOnlyList<MediaTimelineItem>> GetMediaProcessingItemsAsync(
        string? status = null,
        string? mediaType = null,
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"SkipCount={skipCount}",
            $"MaxResultCount={maxResultCount}"
        };
        AddQuery(query, "Status", status);
        AddQuery(query, "MediaType", mediaType);

        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/media/processing-status?" + string.Join("&", query),
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<MediaTimelineItemDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToMediaTimelineItem).ToList() ?? [];
    }

    public async Task<MediaDetail> RetryMediaProcessingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/media/{id}/retry-processing",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var detail = JsonSerializer.Deserialize<MediaDetailDto>(responseText, JsonOptions)
                     ?? throw new InvalidOperationException("Retry media processing response is invalid.");
        return ToMediaDetail(detail);
    }

    public async Task<IReadOnlyList<MediaAlbum>> GetMediaAlbumsAsync(
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"/api/file-center/media/albums?SkipCount={skipCount}&MaxResultCount={maxResultCount}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<MediaAlbumDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToMediaAlbum).ToList() ?? [];
    }

    public Task<MediaAlbum> CreateMediaAlbumAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        return SendMediaAlbumAsync(
            HttpMethod.Post,
            "/api/file-center/media/albums",
            new MediaAlbumRequest { Name = name, Description = description },
            cancellationToken);
    }

    public Task<MediaAlbum> UpdateMediaAlbumAsync(
        Guid id,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        return SendMediaAlbumAsync(
            HttpMethod.Put,
            $"/api/file-center/media/albums/{id}",
            new MediaAlbumRequest { Name = name, Description = description },
            cancellationToken);
    }

    public async Task DeleteMediaAlbumAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            $"/api/file-center/media/albums/{id}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    public async Task<IReadOnlyList<MediaTimelineItem>> GetMediaAlbumItemsAsync(
        Guid id,
        int skipCount = 0,
        int maxResultCount = 60,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"/api/file-center/media/albums/{id}/items?SkipCount={skipCount}&MaxResultCount={maxResultCount}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<MediaTimelineItemDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToMediaTimelineItem).ToList() ?? [];
    }

    public async Task<IReadOnlyList<MediaTimelineItem>> AddMediaAlbumItemsAsync(
        Guid id,
        IReadOnlyCollection<Guid> fileNodeIds,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/media/albums/{id}/items",
            cancellationToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new AddMediaAlbumItemsRequest { FileNodeIds = fileNodeIds.ToList() }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<IReadOnlyList<MediaTimelineItemDto>>(responseText, JsonOptions);
        return result?.Select(ToMediaTimelineItem).ToList() ?? [];
    }

    public async Task RemoveMediaAlbumItemAsync(
        Guid id,
        Guid fileNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            $"/api/file-center/media/albums/{id}/items/{fileNodeId}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    public async Task<MediaAlbum> SetMediaAlbumCoverAsync(
        Guid id,
        Guid fileNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/media/albums/{id}/cover",
            cancellationToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new SetMediaAlbumCoverRequest { FileNodeId = fileNodeId }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var album = JsonSerializer.Deserialize<MediaAlbumDto>(responseText, JsonOptions)
                    ?? throw new InvalidOperationException("Set album cover response is invalid.");
        return ToMediaAlbum(album);
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<WechatLoginSettings> GetWechatLoginSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/api/mobile-auth/wechat/settings", cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var settings = JsonSerializer.Deserialize<WechatLoginSettingsDto>(responseText, JsonOptions)
                       ?? throw new InvalidOperationException("WeChat settings response is invalid.");

        return new WechatLoginSettings(
            settings.IsEnabled,
            settings.AppId,
            settings.Scope,
            settings.CallbackScheme,
            settings.AndroidPackageName,
            settings.IosBundleId,
            settings.IosUrlScheme);
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<WechatBinding?> GetWechatBindingAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/mobile-auth/wechat/binding",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        var binding = JsonSerializer.Deserialize<WechatBindingDto>(responseText, JsonOptions);
        return binding == null ? null : ToWechatBinding(binding);
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    public async Task<WechatBinding> BindCurrentWechatAsync(
        string code,
        string? state,
        string? platform,
        string? deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/mobile-auth/wechat/bind-current",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new BindCurrentWechatRequest
            {
                Code = code,
                State = state,
                Platform = platform,
                DeviceIdHash = deviceIdHash
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var binding = JsonSerializer.Deserialize<WechatBindingDto>(responseText, JsonOptions)
                      ?? throw new InvalidOperationException("WeChat binding response is invalid.");

        return ToWechatBinding(binding);
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    public async Task<WechatBinding> BindExistingWechatAsync(
        string bindingTicket,
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(
            new BindExistingWechatRequest
            {
                BindingTicket = bindingTicket,
                UserNameOrEmail = userNameOrEmail,
                Password = password
            },
            JsonOptions);

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(
            "/api/mobile-auth/wechat/bind-existing",
            content,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var binding = JsonSerializer.Deserialize<WechatBindingDto>(responseText, JsonOptions)
                      ?? throw new InvalidOperationException("WeChat binding response is invalid.");

        return ToWechatBinding(binding);
    }

    /// <summary>
    /// 解除第三方身份绑定，并确保账号仍保留可用登录方式。
    /// </summary>
    public async Task UnbindWechatAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            "/api/mobile-auth/wechat/binding",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<ExternalLoginSettings> GetExternalLoginSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/api/mobile-auth/external/settings", cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var settings = JsonSerializer.Deserialize<ExternalLoginSettingsDto>(responseText, JsonOptions)
                       ?? throw new InvalidOperationException("External sign-in settings response is invalid.");

        return new ExternalLoginSettings(
            settings.Providers
                .Select(provider => new ExternalLoginProviderSettings(
                    provider.Provider,
                    provider.DisplayName,
                    provider.IsEnabled,
                    provider.ClientId,
                    provider.AuthorizationEndpoint,
                    provider.Scope,
                    provider.RedirectUri,
                    provider.UsePkce))
                .ToList());
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public async Task<IReadOnlyList<ExternalBinding>> GetExternalBindingsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/mobile-auth/external/bindings",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var bindings = JsonSerializer.Deserialize<List<ExternalBindingDto>>(responseText, JsonOptions);
        return bindings?.Select(ToExternalBinding).ToList() ?? [];
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    public async Task<ExternalBinding> BindCurrentExternalAsync(
        string provider,
        string code,
        string? state,
        string redirectUri,
        string? codeVerifier,
        string? deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/mobile-auth/external/bind-current",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new BindCurrentExternalRequest
            {
                Provider = provider,
                Code = code,
                State = state,
                RedirectUri = redirectUri,
                CodeVerifier = codeVerifier,
                DeviceIdHash = deviceIdHash
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var binding = JsonSerializer.Deserialize<ExternalBindingDto>(responseText, JsonOptions)
                      ?? throw new InvalidOperationException("External binding response is invalid.");

        return ToExternalBinding(binding);
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    public async Task<ExternalBinding> BindExistingExternalAsync(
        string bindingTicket,
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(
            new BindExistingExternalRequest
            {
                BindingTicket = bindingTicket,
                UserNameOrEmail = userNameOrEmail,
                Password = password
            },
            JsonOptions);

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(
            "/api/mobile-auth/external/bind-existing",
            content,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var binding = JsonSerializer.Deserialize<ExternalBindingDto>(responseText, JsonOptions)
                      ?? throw new InvalidOperationException("External binding response is invalid.");

        return ToExternalBinding(binding);
    }

    /// <summary>
    /// 解除第三方身份绑定，并确保账号仍保留可用登录方式。
    /// </summary>
    public async Task UnbindExternalAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Delete,
            $"/api/mobile-auth/external/bindings/{Uri.EscapeDataString(provider)}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    /// <summary>
    /// 查询管理员用户列表（仅 admin 角色可用）。
    /// </summary>
    public async Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/identity/admin/users",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<AdminUserDto>>(responseText, JsonOptions);
        return result?.Items ?? (IReadOnlyList<AdminUserDto>)JsonSerializer.Deserialize<List<AdminUserDto>>(responseText, JsonOptions) ?? [];
    }

    /// <summary>
    /// 查询当前用户的分享风险摘要（无过期分享、公开分享、长期未使用分享的数量和文案）。
    /// </summary>
    public async Task<ShareRiskSummary> GetShareRiskSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/shares/risk-summary",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var dto = JsonSerializer.Deserialize<ShareRiskSummaryDto>(responseText, JsonOptions)
                  ?? throw new InvalidOperationException("Share risk summary response is invalid.");

        return new ShareRiskSummary(
            dto.NoExpiryShareCount,
            dto.PublicShareCount,
            dto.LongUnusedShareCount,
            dto.NoExpiryWarning,
            dto.PublicWarning,
            dto.LongUnusedWarning);
    }

    /// <summary>
    /// 查询回收站存储空间摘要（已用字节数、超过保留天数的项目数和清理建议）。
    /// </summary>
    public async Task<TrashStorageSummary> GetTrashStorageSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            "/api/file-center/trash/storage-summary",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var dto = JsonSerializer.Deserialize<TrashStorageSummaryDto>(responseText, JsonOptions)
                  ?? throw new InvalidOperationException("Trash storage summary response is invalid.");

        return new TrashStorageSummary(
            dto.UsedBytes,
            dto.ItemsOverThresholdCount,
            dto.RetentionDays,
            dto.CleanupSuggestion);
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AuthSessionExpiredException(AppText.SignInRequired);
        }

        var absoluteRequestUri = new Uri(
            new Uri(AppSettings.ApiBaseUrl.TrimEnd('/') + "/"),
            requestUri.TrimStart('/'));
        var request = new HttpRequestMessage(method, absoluteRequestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return request;
    }

    private async Task SendBatchNoContentAsync<TRequest>(
        string route,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, route, cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    private async Task<IReadOnlyList<CloudDriveItem>> SendBatchItemsAsync<TRequest>(
        string route,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, route, cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<IReadOnlyList<FileNodeDto>>(responseText, JsonOptions);
        return result?.Select(ToCloudDriveItem).ToList() ?? [];
    }

    private static string BuildFolderListPath(
        Guid? parentId,
        int skipCount,
        int maxResultCount,
        CloudDriveQueryOptions? options)
    {
        var query = new List<string>
        {
            $"SkipCount={skipCount}",
            $"MaxResultCount={maxResultCount}"
        };

        if (parentId.HasValue)
        {
            AddQuery(query, "ParentId", parentId.Value.ToString("D"));
        }

        if (options != null)
        {
            AddQuery(query, "SearchKeyword", options.SearchKeyword);
            AddQuery(query, "SearchScope", options.SearchScope);
            AddQuery(query, "NodeType", options.NodeType);
            AddQuery(query, "MediaType", options.MediaType);
            AddQuery(query, "Sorting", options.Sorting);
        }

        return "/api/app/file-center-folders?" + string.Join("&", query);
    }

    private static void AddQuery(List<string> query, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
    }

    private async Task<IReadOnlyList<CloudDriveItem>> GetMediaItemsAsync(
        string route,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            $"{route}?SkipCount={skipCount}&MaxResultCount={maxResultCount}",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var result = JsonSerializer.Deserialize<PagedResult<FileNodeDto>>(responseText, JsonOptions);
        return result?.Items.Select(ToCloudDriveItem).ToList() ?? [];
    }

    private async Task<MediaAlbum> SendMediaAlbumAsync<TRequest>(
        HttpMethod method,
        string route,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(method, route, cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        var album = JsonSerializer.Deserialize<MediaAlbumDto>(responseText, JsonOptions)
                    ?? throw new InvalidOperationException("Media album response is invalid.");
        return ToMediaAlbum(album);
    }

    private static string BuildMediaTimelineQuery(
        int skipCount,
        int maxResultCount,
        string? mediaType,
        Guid? albumId,
        string? processStatus)
    {
        var query = new List<string>
        {
            $"SkipCount={skipCount}",
            $"MaxResultCount={maxResultCount}"
        };

        AddQuery(query, "MediaType", mediaType);
        AddQuery(query, "AlbumId", albumId?.ToString("D"));
        AddQuery(query, "ProcessStatus", processStatus);

        return string.Join("&", query);
    }

    private async Task UploadSmallFileAsync(
        Guid? parentId,
        FileResult file,
        Stream stream,
        IProgress<UploadTransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        var fileSize = stream.CanSeek ? stream.Length : 0;
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/file-center/files/upload-small",
            cancellationToken);

        using var form = new MultipartFormDataContent();
        if (parentId.HasValue)
        {
            form.Add(new StringContent(parentId.Value.ToString()), "ParentId");
        }

        using var fileContent = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        }

        form.Add(fileContent, "File", file.FileName);
        request.Content = form;

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
        progress?.Report(new UploadTransferProgress(1, totalBytes: fileSize, uploadedBytes: fileSize, progressPercent: 100, statusReason: "Completed", nextAction: "OpenFile"));
    }

    private async Task UploadChunkedFileAsync(
        Guid? parentId,
        FileResult file,
        Stream stream,
        long fileSize,
        IProgress<UploadTransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        var totalChunks = (int)Math.Ceiling(fileSize / (double)ChunkSize);
        var session = await CreateUploadSessionAsync(
            parentId,
            file,
            fileSize,
            totalChunks,
            cancellationToken);

        progress?.Report(ToTransferProgress(session, fileSize));

        if (IsCompletedSession(session))
        {
            return;
        }

        var buffer = new byte[ChunkSize];
        var uploadedBytes = Math.Clamp(session.UploadedBytes, 0, fileSize);
        var startChunkIndex = Math.Clamp(session.UploadedChunkCount, 0, totalChunks);

        if (stream.CanSeek && uploadedBytes > 0)
        {
            stream.Seek(uploadedBytes, SeekOrigin.Begin);
        }

        for (var chunkIndex = startChunkIndex; chunkIndex < totalChunks; chunkIndex++)
        {
            var expectedBytes = (int)Math.Min(ChunkSize, fileSize - uploadedBytes);
            var readBytes = await ReadChunkAsync(stream, buffer, expectedBytes, cancellationToken);

            await UploadChunkAsync(
                session.Id,
                chunkIndex,
                buffer,
                readBytes,
                cancellationToken);

            uploadedBytes += readBytes;
            progress?.Report(new UploadTransferProgress(
                uploadedBytes / (double)fileSize,
                chunkIndex + 1,
                uploadedBytes,
                fileSize,
                (decimal)(uploadedBytes * 100d / fileSize),
                true,
                "WaitingForChunks",
                null,
                "UploadMissingChunks"));
        }

        await CompleteUploadSessionAsync(session.Id, cancellationToken);
    }

    private async Task<UploadSessionDto> CreateUploadSessionAsync(
        Guid? parentId,
        FileResult file,
        long fileSize,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            "/api/file-center/upload-sessions",
            cancellationToken);

        var body = JsonSerializer.Serialize(
            new CreateUploadSessionRequest
            {
                ParentId = parentId,
                FileName = file.FileName,
                TotalSize = fileSize,
                ChunkSize = ChunkSize,
                TotalChunks = totalChunks,
                ContentType = file.ContentType
            },
            JsonOptions);

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);

        return JsonSerializer.Deserialize<UploadSessionDto>(responseText, JsonOptions)
               ?? throw new InvalidOperationException("Upload session response is invalid.");
    }

    private async Task UploadChunkAsync(
        Guid sessionId,
        int chunkIndex,
        byte[] buffer,
        int count,
        CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Put,
            $"/api/file-center/upload-sessions/{sessionId}/chunks/{chunkIndex}",
            cancellationToken);

        using var form = new MultipartFormDataContent();
        using var chunkContent = new ByteArrayContent(buffer, 0, count);
        chunkContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(chunkContent, "Chunk", $"{chunkIndex}.part");
        request.Content = form;

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    private async Task CompleteUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Post,
            $"/api/file-center/upload-sessions/{sessionId}/complete",
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseText);
    }

    private static async Task<int> ReadChunkAsync(
        Stream stream,
        byte[] buffer,
        int expectedBytes,
        CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < expectedBytes)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(totalRead, expectedBytes - totalRead),
                cancellationToken);

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    private static UploadTransferProgress ToTransferProgress(UploadSessionDto session, long totalBytes)
    {
        var uploadedBytes = Math.Clamp(session.UploadedBytes, 0, Math.Max(0, totalBytes));
        var progress = session.ProgressPercent.HasValue
            ? (double)session.ProgressPercent.Value / 100d
            : totalBytes > 0
                ? uploadedBytes / (double)totalBytes
                : 0;

        return new UploadTransferProgress(
            progress,
            session.UploadedChunkCount,
            uploadedBytes,
            totalBytes,
            session.ProgressPercent,
            session.IsRetryable,
            session.StatusReason,
            session.FailureReason,
            session.NextAction);
    }

    private static bool IsCompletedSession(UploadSessionDto session)
    {
        return string.Equals(session.StatusReason, "Completed", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(session.NextAction, "OpenFile", StringComparison.OrdinalIgnoreCase);
    }

    private static CloudDriveItem ToCloudDriveItem(FileNodeDto node)
    {
        var isFolder = node.NodeType == FileNodeType.Folder;
        var kind = isFolder ? "Folder" : GetFileKind(node.Name, node.ContentType);
        var badge = isFolder ? "DIR" : GetBadge(kind);

        return new CloudDriveItem(
            node.Id,
            node.ParentId,
            node.Name,
            kind,
            isFolder ? "--" : FormatSize(node.Size),
            AppText.FormatDate(node.LastModificationTime ?? node.CreationTime),
            badge,
            node.ContentType,
            node.IsFavorite);
    }

    private static CloudDriveShare ToCloudDriveShare(FileShareDto share)
    {
        return new CloudDriveShare(
            share.Id,
            share.FileNodeId,
            share.FileName,
            share.Token,
            share.ExpirationTime,
            share.CreationTime,
            share.AllowDownload,
            share.RequiresPassword,
            share.VisitCount,
            share.IsEnabled,
            share.IsExpired);
    }

    private static MediaTimelineItem ToMediaTimelineItem(MediaTimelineItemDto item)
    {
        return new MediaTimelineItem(
            item.Id,
            item.Name,
            item.MediaType,
            item.Size,
            item.ContentType,
            item.TimelineTime,
            item.CreationTime,
            item.ThumbnailBlobObjectId,
            item.ProcessStatus,
            item.ProcessErrorSummary,
            item.Width,
            item.Height,
            item.DurationMilliseconds,
            item.IsFavorite);
    }

    private static MediaDetail ToMediaDetail(MediaDetailDto detail)
    {
        return new MediaDetail(
            detail.FileNodeId,
            detail.Name,
            detail.MediaType,
            detail.Size,
            detail.ContentType,
            detail.Width,
            detail.Height,
            detail.DurationMilliseconds,
            detail.Codec,
            detail.TakenAt,
            detail.ThumbnailBlobObjectId,
            detail.PreviewBlobObjectId,
            detail.ProcessStatus,
            detail.ProcessErrorSummary,
            detail.CanPreview,
            detail.CanRetryProcessing);
    }

    private static MediaAlbum ToMediaAlbum(MediaAlbumDto album)
    {
        return new MediaAlbum(
            album.Id,
            album.Name,
            album.Description,
            album.CoverFileNodeId,
            album.CoverThumbnailBlobObjectId,
            album.ItemsCount,
            album.CreationTime,
            album.LastModificationTime);
    }

    private static WechatBinding ToWechatBinding(WechatBindingDto binding)
    {
        return new WechatBinding(
            binding.Id,
            binding.TenantId,
            binding.UserId,
            binding.AppId,
            binding.NickName,
            binding.AvatarUrl,
            binding.IsEnabled,
            binding.LastLoginTime,
            binding.CreationTime);
    }

    private static ExternalBinding ToExternalBinding(ExternalBindingDto binding)
    {
        return new ExternalBinding(
            binding.Id,
            binding.TenantId,
            binding.UserId,
            binding.Provider,
            binding.Email,
            binding.DisplayName,
            binding.AvatarUrl,
            binding.IsEnabled,
            binding.LastLoginTime,
            binding.CreationTime);
    }

    private static string GetFileKind(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Image";
            }

            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return "Video";
            }

            if (contentType == "application/pdf")
            {
                return "PDF";
            }
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => "Image",
            ".mp4" or ".mov" or ".m4v" or ".mkv" or ".webm" => "Video",
            ".pdf" => "PDF",
            ".zip" or ".rar" or ".7z" => "Archive",
            ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => "Document",
            _ => "File"
        };
    }

    private static string GetBadge(string kind)
    {
        return kind switch
        {
            "Image" => "IMG",
            "Video" => "VID",
            "Archive" => "ZIP",
            "Document" => "DOC",
            "PDF" => "PDF",
            _ => "FILE"
        };
    }

    private static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{size} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }

    private static string BuildCacheFilePath(Guid id, string fileName, string? contentType)
    {
        var safeFileName = SanitizeCacheFileName(fileName);
        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GetExtensionFromContentType(contentType);
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Path.Combine(FileSystem.CacheDirectory, $"pcd-media-{id:N}-{timestamp}{extension}");
    }

    private static string SanitizeCacheFileName(string fileName)
    {
        var safeFileName = string.IsNullOrWhiteSpace(fileName)
            ? "media"
            : fileName;

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(invalidChar, '_');
        }

        return safeFileName;
    }

    private static string GetExtensionFromContentType(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/webm" => ".webm",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            _ => ".bin"
        };
    }

    private static HttpClientHandler CreateHttpClientHandler()
    {
        return new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
    }

    private static string GetApiError(string responseText)
    {
        try
        {
            var error = JsonSerializer.Deserialize<AbpRemoteServiceErrorResponse>(responseText, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Error?.Message))
            {
                return error.Error.Message;
            }
        }
        catch
        {
            // Fall through to the raw response body.
        }

        return string.IsNullOrWhiteSpace(responseText)
            ? "FileCenter request failed."
            : responseText;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string responseText)
    {
        if (IsAuthenticationFailure(response))
        {
            throw new AuthSessionExpiredException(AppText.SignInRequired);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetApiError(responseText));
        }
    }

    private static bool IsAuthenticationFailure(HttpResponseMessage response)
    {
        return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ||
               IsLoginRedirect(response);
    }

    private static bool IsLoginRedirect(HttpResponseMessage response)
    {
        if (response.StatusCode is not (
                HttpStatusCode.MovedPermanently or
                HttpStatusCode.Redirect or
                HttpStatusCode.RedirectMethod or
                HttpStatusCode.TemporaryRedirect or
                HttpStatusCode.PermanentRedirect))
        {
            return false;
        }

        var location = response.Headers.Location?.ToString();
        return !string.IsNullOrWhiteSpace(location) &&
               (location.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("/Error?httpStatusCode=401", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("/Error?httpStatusCode=403", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];

        public long TotalCount { get; init; }
    }

    private sealed class FileNodeDto
    {
        public Guid Id { get; init; }

        public Guid? ParentId { get; init; }

        public FileNodeType NodeType { get; init; }

        public string Name { get; init; } = string.Empty;

        public long Size { get; init; }

        public string? ContentType { get; init; }

        public bool IsFavorite { get; init; }

        public DateTime CreationTime { get; init; }

        public DateTime? LastModificationTime { get; init; }
    }

    private sealed class MediaTimelineItemDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public MediaAssetMediaType MediaType { get; init; }

        public long Size { get; init; }

        public string? ContentType { get; init; }

        public DateTime TimelineTime { get; init; }

        public DateTime CreationTime { get; init; }

        public Guid? ThumbnailBlobObjectId { get; init; }

        public MediaAssetProcessStatus ProcessStatus { get; init; }

        public string? ProcessErrorSummary { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public long? DurationMilliseconds { get; init; }

        public bool IsFavorite { get; init; }
    }

    private sealed class MediaDetailDto
    {
        public Guid FileNodeId { get; init; }

        public string Name { get; init; } = string.Empty;

        public MediaAssetMediaType MediaType { get; init; }

        public long Size { get; init; }

        public string? ContentType { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public long? DurationMilliseconds { get; init; }

        public string? Codec { get; init; }

        public DateTime? TakenAt { get; init; }

        public Guid? ThumbnailBlobObjectId { get; init; }

        public Guid? PreviewBlobObjectId { get; init; }

        public MediaAssetProcessStatus ProcessStatus { get; init; }

        public string? ProcessErrorSummary { get; init; }

        public bool CanPreview { get; init; }

        public bool CanRetryProcessing { get; init; }
    }

    private sealed class MediaAlbumDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public Guid? CoverFileNodeId { get; init; }

        public Guid? CoverThumbnailBlobObjectId { get; init; }

        public int ItemsCount { get; init; }

        public DateTime CreationTime { get; init; }

        public DateTime? LastModificationTime { get; init; }
    }

    private sealed class MediaAlbumRequest
    {
        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }
    }

    private sealed class AddMediaAlbumItemsRequest
    {
        public List<Guid> FileNodeIds { get; init; } = [];
    }

    private sealed class SetMediaAlbumCoverRequest
    {
        public Guid FileNodeId { get; init; }
    }

    private sealed class CreateFolderRequest
    {
        public Guid? ParentId { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    private sealed class CreateUploadSessionRequest
    {
        public Guid? ParentId { get; init; }

        public string FileName { get; init; } = string.Empty;

        public long TotalSize { get; init; }

        public int ChunkSize { get; init; }

        public int TotalChunks { get; init; }

        public string? ContentType { get; init; }
    }

    private sealed class CreateTagRequest
    {
        public string Name { get; init; } = string.Empty;

        public string? Color { get; init; }
    }

    private sealed class SetFavoriteRequest
    {
        public bool IsFavorite { get; init; }
    }

    private class BatchFileNodeRequest
    {
        public List<Guid> Ids { get; init; } = [];
    }

    private sealed class BatchMoveFileNodesRequest : BatchFileNodeRequest
    {
        public Guid? ParentId { get; init; }
    }

    private sealed class BatchSetFavoriteRequest : BatchFileNodeRequest
    {
        public bool IsFavorite { get; init; }
    }

    private sealed class CreateShareRequest
    {
        public Guid FileNodeId { get; init; }

        public DateTime? ExpirationTime { get; init; }

        public bool AllowDownload { get; init; }

        public string? Password { get; init; }
    }

    private sealed class FileTagDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Color { get; init; }
    }

    private sealed class FileShareDto
    {
        public Guid Id { get; init; }

        public Guid FileNodeId { get; init; }

        public string FileName { get; init; } = string.Empty;

        public string Token { get; init; } = string.Empty;

        public DateTime? ExpirationTime { get; init; }

        public DateTime CreationTime { get; init; }

        public bool AllowDownload { get; init; }

        public bool RequiresPassword { get; init; }

        public int VisitCount { get; init; }

        public bool IsEnabled { get; init; }

        public bool IsExpired { get; init; }
    }

    private sealed class StorageUsageDto
    {
        public long UsedBytes { get; init; }

        public long QuotaBytes { get; init; }

        public long RemainingBytes { get; init; }

        public decimal UsagePercent { get; init; }

        public bool IsQuotaConfigured { get; init; }

        public long MaxSingleFileSize { get; init; }
    }

    private sealed class SystemHealthDto
    {
        public SystemHealthStatus OverallStatus { get; init; }

        public SystemHealthStatus ApiStatus { get; init; }

        public SystemHealthStatus DatabaseStatus { get; init; }

        public SystemHealthStatus RedisStatus { get; init; }

        public SystemHealthStatus StorageStatus { get; init; }

        public SystemHealthStatus FfmpegStatus { get; init; }

        public SystemHealthStatus FfprobeStatus { get; init; }

        public string StorageProvider { get; init; } = string.Empty;

        public string StorageLocationDescription { get; init; } = string.Empty;

        public string BackupScopeDescription { get; init; } = string.Empty;

        public string PrivacyBoundaryDescription { get; init; } = string.Empty;

        public long StorageUsedBytes { get; init; }

        public long StorageQuotaBytes { get; init; }

        public long StorageDiskAvailableBytes { get; init; }

        public long StorageDiskTotalBytes { get; init; }

        public bool IsQuotaConfigured { get; init; }

        public DateTime GeneratedAt { get; init; }

        public List<string> Diagnostics { get; init; } = [];
    }

    private sealed class OperationLogDto
    {
        public Guid Id { get; init; }

        public DateTime Time { get; init; }

        public string Source { get; init; } = string.Empty;

        public string Action { get; init; } = string.Empty;

        public string Result { get; init; } = string.Empty;

        public string? UserName { get; init; }

        public string? ClientIpAddress { get; init; }

        public int? HttpStatusCode { get; init; }

        public string Summary { get; init; } = string.Empty;
    }

    private sealed class UploadSessionDto
    {
        public Guid Id { get; init; }

        public int UploadedChunkCount { get; init; }

        public long UploadedBytes { get; init; }

        public decimal? ProgressPercent { get; init; }

        public bool IsRetryable { get; init; } = true;

        public string? StatusReason { get; init; }

        public string? FailureReason { get; init; }

        public string? NextAction { get; init; }
    }

    private sealed class WechatLoginSettingsDto
    {
        public bool IsEnabled { get; init; }

        public string? AppId { get; init; }

        public string Scope { get; init; } = "snsapi_userinfo";

        public string CallbackScheme { get; init; } = "privateclouddrive";

        public string? AndroidPackageName { get; init; }

        public string? IosBundleId { get; init; }

        public string? IosUrlScheme { get; init; }
    }

    private sealed class WechatBindingDto
    {
        public Guid Id { get; init; }

        public Guid? TenantId { get; init; }

        public Guid UserId { get; init; }

        public string AppId { get; init; } = string.Empty;

        public string? NickName { get; init; }

        public string? AvatarUrl { get; init; }

        public bool IsEnabled { get; init; }

        public DateTime? LastLoginTime { get; init; }

        public DateTime CreationTime { get; init; }
    }

    private sealed class BindCurrentWechatRequest
    {
        public string Code { get; init; } = string.Empty;

        public string? State { get; init; }

        public string? Platform { get; init; }

        public string? DeviceIdHash { get; init; }
    }

    private sealed class BindExistingWechatRequest
    {
        public string BindingTicket { get; init; } = string.Empty;

        public string UserNameOrEmail { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }

    private sealed class ExternalLoginSettingsDto
    {
        public List<ExternalLoginProviderSettingsDto> Providers { get; init; } = [];
    }

    private sealed class ExternalLoginProviderSettingsDto
    {
        public string Provider { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public bool IsEnabled { get; init; }

        public string? ClientId { get; init; }

        public string AuthorizationEndpoint { get; init; } = string.Empty;

        public string Scope { get; init; } = string.Empty;

        public string RedirectUri { get; init; } = string.Empty;

        public bool UsePkce { get; init; }
    }

    private sealed class ExternalBindingDto
    {
        public Guid Id { get; init; }

        public Guid? TenantId { get; init; }

        public Guid UserId { get; init; }

        public string Provider { get; init; } = string.Empty;

        public string? Email { get; init; }

        public string? DisplayName { get; init; }

        public string? AvatarUrl { get; init; }

        public bool IsEnabled { get; init; }

        public DateTime? LastLoginTime { get; init; }

        public DateTime CreationTime { get; init; }
    }

    private sealed class BindCurrentExternalRequest
    {
        public string Provider { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public string? State { get; init; }

        public string RedirectUri { get; init; } = string.Empty;

        public string? CodeVerifier { get; init; }

        public string? DeviceIdHash { get; init; }
    }

    private sealed class BindExistingExternalRequest
    {
        public string BindingTicket { get; init; } = string.Empty;

        public string UserNameOrEmail { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }

    private enum FileNodeType
    {
        Folder = 1,
        File = 2
    }

    private sealed class AbpRemoteServiceErrorResponse
    {
        [JsonPropertyName("error")]
        public AbpRemoteServiceError? Error { get; init; }
    }

    private sealed class AbpRemoteServiceError
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    private sealed class ShareRiskSummaryDto
    {
        public int NoExpiryShareCount { get; init; }

        public int PublicShareCount { get; init; }

        public int LongUnusedShareCount { get; init; }

        public string NoExpiryWarning { get; init; } = string.Empty;

        public string PublicWarning { get; init; } = string.Empty;

        public string LongUnusedWarning { get; init; } = string.Empty;
    }

    private sealed class TrashStorageSummaryDto
    {
        public long UsedBytes { get; init; }

        public int ItemsOverThresholdCount { get; init; }

        public int RetentionDays { get; init; }

        public string CleanupSuggestion { get; init; } = string.Empty;
    }
}
