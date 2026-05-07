using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

public class VerifySharePasswordInput
{
    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
