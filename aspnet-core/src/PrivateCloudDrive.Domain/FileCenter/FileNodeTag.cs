using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

public class FileNodeTag : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid FileNodeId { get; private set; }

    public Guid TagId { get; private set; }

    protected FileNodeTag()
    {
    }

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
