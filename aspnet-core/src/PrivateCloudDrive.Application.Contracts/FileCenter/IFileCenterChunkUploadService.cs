using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 大文件分片上传应用服务契约。
/// 客户端通过会话创建、分片上传、完成合并和取消接口实现断点续传。
/// </summary>
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
