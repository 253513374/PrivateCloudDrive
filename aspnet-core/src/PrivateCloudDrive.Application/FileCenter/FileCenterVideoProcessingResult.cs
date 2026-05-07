namespace PrivateCloudDrive.FileCenter;

public class FileCenterVideoProcessingResult
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required long DurationMilliseconds { get; init; }

    public string? Codec { get; init; }

    public required byte[] ThumbnailBytes { get; init; }

    public string? MetadataJson { get; init; }
}
