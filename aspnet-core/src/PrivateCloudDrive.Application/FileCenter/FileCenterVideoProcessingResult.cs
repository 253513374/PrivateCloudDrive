namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示FileCenterVideoProcessingResult操作结果，用于向调用方返回处理状态和必要业务信息。
/// </summary>
public class FileCenterVideoProcessingResult
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required long DurationMilliseconds { get; init; }

    public string? Codec { get; init; }

    public required byte[] ThumbnailBytes { get; init; }

    public string? MetadataJson { get; init; }
}
