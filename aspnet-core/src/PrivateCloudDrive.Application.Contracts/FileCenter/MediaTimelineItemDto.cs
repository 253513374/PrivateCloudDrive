using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体库时间线项目 DTO。
/// </summary>
public class MediaTimelineItemDto : EntityDto<Guid>
{
    public Guid FileNodeId { get; set; }

    public Guid? MediaAssetId { get; set; }

    public string Name { get; set; } = string.Empty;

    public MediaAssetMediaType MediaType { get; set; }

    public long Size { get; set; }

    public string? ContentType { get; set; }

    public DateTime TimelineTime { get; set; }

    public DateTime CreationTime { get; set; }

    public Guid? ThumbnailBlobObjectId { get; set; }

    public MediaAssetProcessStatus ProcessStatus { get; set; }

    public string? ProcessErrorSummary { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? DurationMilliseconds { get; set; }

    public bool IsFavorite { get; set; }
}
