using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterFileUploadService
{
    Task<FileNodeDto> UploadSmallFileAsync(
        Guid? parentId,
        string fileName,
        string? contentType,
        Stream stream,
        long size,
        CancellationToken cancellationToken = default);
}
