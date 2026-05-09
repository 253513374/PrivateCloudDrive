using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体相册 DTO。
/// </summary>
public class MediaAlbumDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? CoverFileNodeId { get; set; }

    public Guid? CoverThumbnailBlobObjectId { get; set; }

    public int ItemsCount { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastModificationTime { get; set; }
}

/// <summary>
/// 创建媒体相册输入。
/// </summary>
public class CreateMediaAlbumInput
{
    [Required]
    [StringLength(MediaAlbumConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(MediaAlbumConsts.MaxDescriptionLength)]
    public string? Description { get; set; }
}

/// <summary>
/// 更新媒体相册输入。
/// </summary>
public class UpdateMediaAlbumInput : CreateMediaAlbumInput
{
}

/// <summary>
/// 批量加入媒体相册输入。
/// </summary>
public class AddMediaAlbumItemsInput
{
    public List<Guid> FileNodeIds { get; set; } = [];
}

/// <summary>
/// 设置相册封面输入。
/// </summary>
public class SetMediaAlbumCoverInput
{
    public Guid FileNodeId { get; set; }
}
