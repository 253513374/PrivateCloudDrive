using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

public class FileShareDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public Guid FileNodeId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public FileNodeType NodeType { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime? ExpirationTime { get; set; }

    public bool AllowDownload { get; set; }

    public bool RequiresPassword { get; set; }

    public int VisitCount { get; set; }

    public bool IsEnabled { get; set; }
}
