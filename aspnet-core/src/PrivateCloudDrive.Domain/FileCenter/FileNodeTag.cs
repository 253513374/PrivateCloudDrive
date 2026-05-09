using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心FileNodeTag，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class FileNodeTag : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid FileNodeId { get; private set; }

    public Guid TagId { get; private set; }

    protected FileNodeTag()
    {
    }

    /// <summary>
    /// 初始化 <see cref="FileNodeTag"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileNodeTag(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid fileNodeId,
        Guid tagId)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        FileNodeId = fileNodeId;
        TagId = tagId;
    }
}
