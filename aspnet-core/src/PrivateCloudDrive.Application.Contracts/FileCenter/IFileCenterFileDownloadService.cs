using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件下载与缩略图读取应用服务契约。
/// </summary>
public interface IFileCenterFileDownloadService
{
    /// <summary>
    /// 获取完整原始文件下载流和响应元数据。
    /// </summary>
    Task<FileDownloadInfo> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按可选 Range 获取原始文件下载流和响应元数据。
    /// </summary>
    Task<FileDownloadInfo> GetDownloadAsync(
        Guid id,
        FileDownloadRangeRequest? range,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取媒体缩略图下载流和响应元数据。
    /// </summary>
    Task<FileDownloadInfo> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default);
}
