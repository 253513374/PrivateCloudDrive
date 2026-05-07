using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterChunkUploadService
{
    Task<UploadSessionDto> CreateAsync(CreateUploadSessionInput input);

    Task<UploadSessionDto> GetAsync(Guid id);

    Task<UploadChunkResultDto> UploadChunkAsync(
        Guid id,
        int chunkIndex,
        Stream stream,
        long size,
        CancellationToken cancellationToken = default);

    Task<FileNodeDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid id);
}
