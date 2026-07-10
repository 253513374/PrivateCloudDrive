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

    /// <summary>
    /// 按动作类型筛选（如 "FileUpload"、"FileDelete"），与 Action 同义。
    /// </summary>
    [StringLength(128)]
    public string? ActionName { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// 按文件/文件夹 ID 筛选。
    /// </summary>
    public Guid? FileNodeId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 按创建时间筛选（起始），与 StartTime 同义。
    /// </summary>
    public DateTime? CreateAfter { get; set; }

    /// <summary>
    /// 按创建时间筛选（结束），与 EndTime 同义。
    /// </summary>
    public DateTime? CreateBefore { get; set; }
}
