using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

public class FileNodeDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public Guid? ParentId { get; set; }

    public FileNodeType NodeType { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public long Size { get; set; }

    public string? ContentType { get; set; }

    public string? BlobName { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastModificationTime { get; set; }
}
