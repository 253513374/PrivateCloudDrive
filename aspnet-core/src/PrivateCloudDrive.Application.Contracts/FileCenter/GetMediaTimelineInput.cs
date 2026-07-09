using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体库时间线查询输入。
/// 支持按媒体类型、处理状态、时间范围、收藏和标签过滤。
/// MaxResultCount 上限由服务端检查（当前上限 500）。
/// </summary>
public class GetMediaTimelineInput : PagedResultRequestDto
{
    public const int MaxAllowedResultCount = 500;

    public MediaAssetMediaType? MediaType { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public Guid? AlbumId { get; set; }

    public bool? IsFavorite { get; set; }

    public Guid? TagId { get; set; }

    public MediaAssetProcessStatus? ProcessStatus { get; set; }
}
