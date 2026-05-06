using System;
using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

public class CreateFolderInput
{
    public Guid? ParentId { get; set; }

    [Required]
    [StringLength(FileNodeConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
}
