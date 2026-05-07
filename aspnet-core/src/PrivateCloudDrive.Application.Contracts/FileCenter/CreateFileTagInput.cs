using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

public class CreateFileTagInput
{
    [Required]
    [StringLength(FileTagConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(FileTagConsts.MaxColorLength)]
    public string? Color { get; set; }
}
