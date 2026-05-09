using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示FileTag数据传输对象，用于跨层或 API 边界返回业务数据。
/// </summary>
public class FileTagDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Color { get; set; }
}
