using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

public class FileNode : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid? ParentId { get; private set; }

    public FileNodeType NodeType { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public long Size { get; private set; }

    public string? ContentType { get; private set; }

    public string? BlobName { get; private set; }

    protected FileNode()
    {
    }

    private FileNode(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        FileNodeType nodeType,
        [NotNull] string name,
        long size = 0,
        string? contentType = null,
        string? blobName = null)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        ParentId = parentId;
        NodeType = nodeType;

        SetName(name);
        SetFileMetadata(size, contentType, blobName);
    }

    public static FileNode CreateFolder(Guid id, Guid? tenantId, Guid ownerId, Guid? parentId, string name)
    {
        return new FileNode(id, tenantId, ownerId, parentId, FileNodeType.Folder, name);
    }

    public static FileNode CreateFile(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name,
        long size,
        string? contentType = null,
        string? blobName = null)
    {
        return new FileNode(id, tenantId, ownerId, parentId, FileNodeType.File, name, size, contentType, blobName);
    }

    public void Rename(string name)
    {
        SetName(name);
    }

    public void MoveTo(Guid? parentId)
    {
        ParentId = parentId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeleterId = null;
        DeletionTime = null;
    }

    public void SetFileMetadata(long size, string? contentType = null, string? blobName = null)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "File size cannot be negative.");
        }

        if (NodeType == FileNodeType.Folder && size != 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterFolderCannotHaveSize);
        }

        Size = size;
        ContentType = Check.Length(contentType, nameof(contentType), FileNodeConsts.MaxContentTypeLength);
        BlobName = Check.Length(blobName, nameof(blobName), FileNodeConsts.MaxBlobNameLength);
    }

    public static string NormalizeName(string name)
    {
        return Check.NotNullOrWhiteSpace(name, nameof(name))
            .Trim()
            .ToUpperInvariant();
    }

    private void SetName(string name)
    {
        var trimmedName = Check.NotNullOrWhiteSpace(name, nameof(name)).Trim();

        Name = Check.Length(
            trimmedName,
            nameof(name),
            FileNodeConsts.MaxNameLength)!;

        NormalizedName = Check.Length(
            NormalizeName(Name),
            nameof(name),
            FileNodeConsts.MaxNormalizedNameLength)!;
    }
}
