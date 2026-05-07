using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

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

    public CloudDriveApiClient(IAuthService authService)
    {
        _authService = authService;
    }

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
            FormatDate(node.LastModificationTime ?? node.CreationTime),
            badge,
            node.ContentType);
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

    private static string FormatDate(DateTime dateTime)
    {
        var localTime = dateTime.Kind == DateTimeKind.Utc
            ? dateTime.ToLocalTime()
            : dateTime;

        return localTime.ToString("MMM d");
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

    private sealed class UploadSessionDto
    {
        public Guid Id { get; init; }
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
