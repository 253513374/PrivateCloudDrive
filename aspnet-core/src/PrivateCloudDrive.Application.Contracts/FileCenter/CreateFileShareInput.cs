using System;
using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示CreateFileShare请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class CreateFileShareInput
{
    public Guid FileNodeId { get; set; }

    public DateTime? ExpirationTime { get; set; }

    public bool AllowDownload { get; set; } = true;

    [StringLength(128)]
    public string? Password { get; set; }
}
