using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体库时间线查询输入。
/// </summary>
public class GetMediaTimelineInput : PagedResultRequestDto
{
    public MediaAssetMediaType? MediaType { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public Guid? AlbumId { get; set; }

    public bool? IsFavorite { get; set; }

    public Guid? TagId { get; set; }

    public MediaAssetProcessStatus? ProcessStatus { get; set; }
}
