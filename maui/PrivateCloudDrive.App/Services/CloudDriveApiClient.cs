using System.Net.Http.Headers;
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
    private readonly HttpClient _httpClient = new()
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
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/app/file-center-folders?SkipCount={skipCount}&MaxResultCount={maxResultCount}";
        if (parentId.HasValue)
        {
            path += $"&ParentId={parentId.Value:D}";
        }

        using var request = await CreateAuthenticatedRequestAsync(
            HttpMethod.Get,
            path,
            cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetApiError(responseText));
        }

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
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = await file.OpenReadAsync();
        var fileSize = stream.CanSeek ? stream.Length : 0;

        progress?.Report(0);

        if (!stream.CanSeek || fileSize <= SmallUploadThreshold)
        {
            await UploadSmallFileAsync(parentId, file, stream, progress, cancellationToken);
            progress?.Report(1);
            return;
        }

        await UploadChunkedFileAsync(parentId, file, stream, fileSize, progress, cancellationToken);
        progress?.Report(1);
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
            throw new InvalidOperationException(GetApiError(responseText));
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
            throw new InvalidOperationException("Sign in is required.");
        }

        return new RemoteFileContentSource(
            new Uri(_httpClient.BaseAddress!, $"/api/file-center/files/{id}/content"),
            new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {accessToken}"
            });
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
            share.AllowDownload,
            share.RequiresPassword);
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

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Sign in is required.");
        }

        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return request;
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

    private async Task UploadSmallFileAsync(
        Guid? parentId,
        FileResult file,
        Stream stream,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
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
        progress?.Report(1);
    }

    private async Task UploadChunkedFileAsync(
        Guid? parentId,
        FileResult file,
        Stream stream,
        long fileSize,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var totalChunks = (int)Math.Ceiling(fileSize / (double)ChunkSize);
        var session = await CreateUploadSessionAsync(
            parentId,
            file,
            fileSize,
            totalChunks,
            cancellationToken);

        var buffer = new byte[ChunkSize];
        long uploadedBytes = 0;

        for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
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
            progress?.Report(uploadedBytes / (double)fileSize);
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
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetApiError(responseText));
        }
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

        public bool AllowDownload { get; init; }

        public bool RequiresPassword { get; init; }
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
}
