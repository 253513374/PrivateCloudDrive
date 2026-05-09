using System;
using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示CreateFolder请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class CreateFolderInput
{
    public Guid? ParentId { get; set; }

    [Required]
    [StringLength(FileNodeConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
}
