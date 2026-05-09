using System.IO;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示FileCenterMediaProcessing配置选项，用于集中管理运行时可调整参数。
/// </summary>
public class FileCenterMediaProcessingOptions
{
    public string FfprobePath { get; set; } = "ffprobe";

    public string FfmpegPath { get; set; } = "ffmpeg";

    public string? TempRootPath { get; set; }

    public int VideoThumbnailWidth { get; set; } = 320;

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public string GetTempRootPath(string storageRootPath)
    {
        return string.IsNullOrWhiteSpace(TempRootPath)
            ? Path.Combine(storageRootPath, "temp", "media-processing")
            : TempRootPath;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public int GetVideoThumbnailWidth()
    {
        return VideoThumbnailWidth > 0 ? VideoThumbnailWidth : 320;
    }
}
