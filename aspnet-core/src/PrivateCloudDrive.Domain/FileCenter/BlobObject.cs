using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

public class BlobObject : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string BlobName { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    public long Size { get; private set; }

    public string? ContentType { get; private set; }

    public string? Hash { get; private set; }

    protected BlobObject()
    {
    }

    private BlobObject(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        string blobName,
        string fileName,
        long size,
        string? contentType = null,
        string? hash = null)
        : base(id)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Blob size cannot be negative.");
        }

        TenantId = tenantId;
        OwnerId = ownerId;
        Size = size;
        BlobName = Check.Length(
            Check.NotNullOrWhiteSpace(blobName, nameof(blobName)),
            nameof(blobName),
            BlobObjectConsts.MaxBlobNameLength)!;
        FileName = Check.Length(
            Check.NotNullOrWhiteSpace(fileName, nameof(fileName)),
            nameof(fileName),
            BlobObjectConsts.MaxFileNameLength)!;
        ContentType = Check.Length(contentType, nameof(contentType), BlobObjectConsts.MaxContentTypeLength);
        Hash = Check.Length(hash, nameof(hash), BlobObjectConsts.MaxHashLength);
    }

    public static BlobObject Create(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        string blobName,
        string fileName,
        long size,
        string? contentType = null,
        string? hash = null)
    {
        return new BlobObject(id, tenantId, ownerId, blobName, fileName, size, contentType, hash);
    }
}
