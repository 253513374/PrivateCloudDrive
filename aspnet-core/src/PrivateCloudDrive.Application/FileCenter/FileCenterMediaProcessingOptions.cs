using System.IO;

namespace PrivateCloudDrive.FileCenter;

public class FileCenterMediaProcessingOptions
{
    public string FfprobePath { get; set; } = "ffprobe";

    public string FfmpegPath { get; set; } = "ffmpeg";

    public string? TempRootPath { get; set; }

    public int VideoThumbnailWidth { get; set; } = 320;

    public string GetTempRootPath(string storageRootPath)
    {
        return string.IsNullOrWhiteSpace(TempRootPath)
            ? Path.Combine(storageRootPath, "temp", "media-processing")
            : TempRootPath;
    }

    public int GetVideoThumbnailWidth()
    {
        return VideoThumbnailWidth > 0 ? VideoThumbnailWidth : 320;
    }
}
