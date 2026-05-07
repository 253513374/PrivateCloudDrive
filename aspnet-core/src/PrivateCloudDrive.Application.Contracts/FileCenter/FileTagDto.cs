using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

public class FileTagDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Color { get; set; }
}
