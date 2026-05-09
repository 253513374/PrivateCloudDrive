using System;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心MediaAssetProcessingJobArgs，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Serializable]
public class MediaAssetProcessingJobArgs
{
    public Guid MediaAssetId { get; set; }

    public Guid FileNodeId { get; set; }
}
