using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体处理任务查询输入。
/// </summary>
public class GetMediaTasksInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// 按处理状态筛选（Pending / Processing / Completed / Failed）。
    /// </summary>
    public string? ProcessStatus { get; set; }
}
