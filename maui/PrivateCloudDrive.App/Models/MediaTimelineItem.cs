namespace PrivateCloudDrive.App.Models;

public sealed record MediaTimelineItem(
    Guid Id,
    string Name,
    MediaAssetMediaType MediaType,
    long Size,
    string? ContentType,
    DateTime TimelineTime,
    DateTime CreationTime,
    Guid? ThumbnailBlobObjectId,
    MediaAssetProcessStatus ProcessStatus,
    string? ProcessErrorSummary,
    int? Width,
    int? Height,
    long? DurationMilliseconds,
    bool IsFavorite);
