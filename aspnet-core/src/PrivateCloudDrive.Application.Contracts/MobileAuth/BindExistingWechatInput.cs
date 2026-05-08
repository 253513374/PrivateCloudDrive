using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.MobileAuth;

public class BindExistingWechatInput
{
    [Required]
    [StringLength(128)]
    public string BindingTicket { get; set; } = string.Empty;

    [Required]
    [StringLength(MobileAuthAuditLogConsts.MaxUserNameLength)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Password { get; set; } = string.Empty;
}
