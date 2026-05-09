using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 首次第三方登录后，把临时绑定票据绑定到已有账号的输入参数。
/// </summary>
public class BindExistingExternalLoginInput
{
    /// <summary>
    /// 未绑定第三方账号首次登录时服务端签发的短期票据。
    /// </summary>
    [Required]
    [StringLength(128)]
    public string BindingTicket { get; set; } = string.Empty;

    /// <summary>
    /// 要绑定的 PrivateCloudDrive 账号用户名或邮箱。
    /// </summary>
    [Required]
    [StringLength(MobileAuthAuditLogConsts.MaxUserNameLength)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// 账号密码校验用密码；只参与校验，不允许出现在日志、审计或响应中。
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Password { get; set; } = string.Empty;
}
