namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心UploadSessionStatus，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public enum UploadSessionStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2
}
