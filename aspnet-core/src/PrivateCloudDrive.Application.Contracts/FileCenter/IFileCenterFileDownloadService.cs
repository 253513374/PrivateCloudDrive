using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件下载与缩略图读取应用服务契约。
/// </summary>
public interface IFileCenterFileDownloadService
{
    Task<FileDownloadInfo> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FileDownloadInfo> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default);
}
