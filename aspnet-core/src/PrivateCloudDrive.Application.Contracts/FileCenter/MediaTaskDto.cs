using System;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体处理任务 DTO。
/// </summary>
public class MediaTaskDto
{
    /// <summary>
    /// 任务 ID。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 文件节点 ID。
    /// </summary>
    public Guid FileNodeId { get; set; }

    /// <summary>
    /// 媒体类型（Image / Video）。
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// 处理状态（Pending / Processing / Completed / Failed）。
    /// </summary>
    public string ProcessStatus { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因（仅 Failed 状态时有效）。
    /// </summary>
    public string? ProcessError { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 最后修改时间。
    /// </summary>
    public DateTime? LastModificationTime { get; set; }
}
