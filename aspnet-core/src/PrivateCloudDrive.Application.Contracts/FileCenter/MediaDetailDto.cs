using System;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体详情 DTO。
/// </summary>
public class MediaDetailDto
{
    public Guid FileNodeId { get; set; }

    public Guid? MediaAssetId { get; set; }

    public string Name { get; set; } = string.Empty;

    public MediaAssetMediaType MediaType { get; set; }

    public long Size { get; set; }

    public string? ContentType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? DurationMilliseconds { get; set; }

    public string? Codec { get; set; }

    public DateTime? TakenAt { get; set; }

    public Guid? ThumbnailBlobObjectId { get; set; }

    public Guid? PreviewBlobObjectId { get; set; }

    public MediaAssetProcessStatus ProcessStatus { get; set; }

    public string? ProcessErrorSummary { get; set; }

    public bool CanPreview { get; set; }

    public bool CanRetryProcessing { get; set; }
}
