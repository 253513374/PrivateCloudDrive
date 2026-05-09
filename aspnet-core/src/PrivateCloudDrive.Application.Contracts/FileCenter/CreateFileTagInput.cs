using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示CreateFileTag请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class CreateFileTagInput
{
    [Required]
    [StringLength(FileTagConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(FileTagConsts.MaxColorLength)]
    public string? Color { get; set; }
}
