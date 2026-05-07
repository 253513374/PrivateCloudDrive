using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterFileDownloadService
{
    Task<FileDownloadInfo> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FileDownloadInfo> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default);
}
