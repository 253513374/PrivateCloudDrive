using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体库分页查询输入，支持按收藏状态和标签过滤。
/// </summary>
public class GetMediaFilesInput : PagedResultRequestDto
{
    public Guid? TagId { get; set; }

    public bool? IsFavorite { get; set; }
}
