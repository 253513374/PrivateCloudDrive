using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 单个分片上传后的结果，返回服务端已确认的分片索引。
/// </summary>
public class UploadChunkResultDto
{
    public IReadOnlyList<int> UploadedChunks { get; set; } = new List<int>();
}
