using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.OperationLogs;

/// <summary>
/// 表示GetOperationLogs请求输入参数，用于约束客户端提交的数据。
/// </summary>
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
