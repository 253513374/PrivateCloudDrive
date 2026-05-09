namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心媒体类型筛选条件，用于在普通文件列表中快速过滤图片、视频或其他文件。
/// </summary>
public enum FileCenterMediaTypeFilter
{
    /// <summary>
    /// 图片文件，通常依据 ContentType 的 image/ 前缀判断。
    /// </summary>
    Image = 0,

    /// <summary>
    /// 视频文件，通常依据 ContentType 的 video/ 前缀判断。
    /// </summary>
    Video = 1,

    /// <summary>
    /// 非图片、非视频的其他普通文件。
    /// </summary>
    Other = 2
}
