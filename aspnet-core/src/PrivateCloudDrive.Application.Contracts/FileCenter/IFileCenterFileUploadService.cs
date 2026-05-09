using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 小文件直传应用服务契约。
/// </summary>
public interface IFileCenterFileUploadService
{
    Task<FileNodeDto> UploadSmallFileAsync(
        Guid? parentId,
        string fileName,
        string? contentType,
        Stream stream,
        long size,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
