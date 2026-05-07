using System.IO;

namespace PrivateCloudDrive.FileCenter;

public class FileDownloadInfo
{
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    public Stream Content { get; set; } = null!;
}
