namespace PrivateCloudDrive.App.Models;

public sealed record MediaDetail(
    Guid FileNodeId,
    string Name,
    MediaAssetMediaType MediaType,
    long Size,
    string? ContentType,
    int? Width,
    int? Height,
    long? DurationMilliseconds,
    string? Codec,
    DateTime? TakenAt,
    Guid? ThumbnailBlobObjectId,
    Guid? PreviewBlobObjectId,
    MediaAssetProcessStatus ProcessStatus,
    string? ProcessErrorSummary,
    bool CanPreview,
    bool CanRetryProcessing);
