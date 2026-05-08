using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.MobileAuth;

public class MobileAuthAuditLogDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string? FailureReason { get; set; }

    public string? ClientId { get; set; }

    public string? DeviceIdHash { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreationTime { get; set; }
}
