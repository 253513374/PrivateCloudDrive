using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示CreateMobileAuthAuditLog请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class CreateMobileAuthAuditLogInput
{
    [Required]
    [StringLength(MobileAuthAuditLogConsts.MaxProviderLength)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [StringLength(MobileAuthAuditLogConsts.MaxActionLength)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(MobileAuthAuditLogConsts.MaxResultLength)]
    public string Result { get; set; } = string.Empty;

    [StringLength(MobileAuthAuditLogConsts.MaxFailureReasonLength)]
    public string? FailureReason { get; set; }

    [StringLength(MobileAuthAuditLogConsts.MaxClientIdLength)]
    public string? ClientId { get; set; }

    [StringLength(MobileAuthAuditLogConsts.MaxUserNameLength)]
    public string? UserName { get; set; }

    [StringLength(MobileAuthAuditLogConsts.MaxDeviceIdHashLength)]
    public string? DeviceIdHash { get; set; }

    [StringLength(MobileAuthAuditLogConsts.MaxUserAgentLength)]
    public string? UserAgent { get; set; }
}
