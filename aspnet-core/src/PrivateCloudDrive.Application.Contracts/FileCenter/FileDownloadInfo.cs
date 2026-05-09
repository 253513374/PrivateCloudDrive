using System.IO;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心FileDownloadInfo，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class FileDownloadInfo
{
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    public Stream Content { get; set; } = null!;
}
