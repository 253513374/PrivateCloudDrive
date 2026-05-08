using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.OperationLogs;

public class GetOperationLogsInput : PagedAndSortedResultRequestDto
{
    [StringLength(64)]
    public string? Source { get; set; }

    [StringLength(128)]
    public string? Action { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(256)]
    public string? UserName { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}
