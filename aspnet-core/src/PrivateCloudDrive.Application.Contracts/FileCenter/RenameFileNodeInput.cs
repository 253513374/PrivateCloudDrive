using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示RenameFileNode请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class RenameFileNodeInput
{
    [Required]
    [StringLength(FileNodeConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
}
