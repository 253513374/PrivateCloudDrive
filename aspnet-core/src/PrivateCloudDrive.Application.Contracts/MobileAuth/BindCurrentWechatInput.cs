using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.MobileAuth;

public class BindCurrentWechatInput
{
    [Required]
    [StringLength(256)]
    public string Code { get; set; } = string.Empty;

    [StringLength(128)]
    public string? State { get; set; }

    [StringLength(WechatUserBindingConsts.MaxPlatformLength)]
    public string? Platform { get; set; }

    [StringLength(MobileAuthAuditLogConsts.MaxDeviceIdHashLength)]
    public string? DeviceIdHash { get; set; }
}
