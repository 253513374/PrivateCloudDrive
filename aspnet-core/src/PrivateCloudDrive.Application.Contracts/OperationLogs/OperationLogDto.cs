using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.OperationLogs;

/// <summary>
/// 表示OperationLog数据传输对象，用于跨层或 API 边界返回业务数据。
/// </summary>
public class OperationLogDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public DateTime Time { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? ClientId { get; set; }

    public string? ClientIpAddress { get; set; }

    public int? HttpStatusCode { get; set; }

    public string? CorrelationId { get; set; }

    public string Summary { get; set; } = string.Empty;
}
