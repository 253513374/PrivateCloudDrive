using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体相册项目关联。删除该关联不会删除原始文件。
/// </summary>
public class MediaAlbumItem : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid AlbumId { get; private set; }

    public Guid FileNodeId { get; private set; }

    public int SortOrder { get; private set; }

    protected MediaAlbumItem()
    {
    }

    /// <summary>
    /// 初始化媒体相册项目关联。
    /// </summary>
    public MediaAlbumItem(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid albumId,
        Guid fileNodeId,
        int sortOrder = 0)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        AlbumId = albumId;
        FileNodeId = fileNodeId;
        SortOrder = sortOrder;
    }
}
