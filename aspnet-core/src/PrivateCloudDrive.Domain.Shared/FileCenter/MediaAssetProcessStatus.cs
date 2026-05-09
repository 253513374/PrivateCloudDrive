namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心MediaAssetProcessStatus，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public enum MediaAssetProcessStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
