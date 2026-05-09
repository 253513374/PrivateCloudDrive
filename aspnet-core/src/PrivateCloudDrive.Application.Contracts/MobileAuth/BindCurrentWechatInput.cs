using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示BindCurrentWechat请求输入参数，用于约束客户端提交的数据。
/// </summary>
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
