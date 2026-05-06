using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

public class RenameFileNodeInput
{
    [Required]
    [StringLength(FileNodeConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
}
