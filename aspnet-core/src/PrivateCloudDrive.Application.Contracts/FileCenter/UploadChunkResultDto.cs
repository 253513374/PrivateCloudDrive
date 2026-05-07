using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

public class UploadChunkResultDto
{
    public IReadOnlyList<int> UploadedChunks { get; set; } = new List<int>();
}
