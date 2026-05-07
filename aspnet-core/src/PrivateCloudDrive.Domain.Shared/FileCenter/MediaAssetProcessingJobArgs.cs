using System;

namespace PrivateCloudDrive.FileCenter;

[Serializable]
public class MediaAssetProcessingJobArgs
{
    public Guid MediaAssetId { get; set; }

    public Guid FileNodeId { get; set; }
}
