using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 已登录用户绑定第三方账号时提交的授权结果。
/// 授权码只在后端换取 Provider 身份，不会写入审计日志或返回给客户端。
/// </summary>
public class BindCurrentExternalLoginInput
{
    /// <summary>
    /// 第三方登录 Provider 名称，例如 Google 或 GitHub。
    /// </summary>
    [Required]
    [StringLength(ExternalUserBindingConsts.MaxProviderLength)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider 回调返回的一次性授权码。
    /// </summary>
    [Required]
    [StringLength(2048)]
    public string Code { get; set; } = string.Empty;

    [StringLength(128)]
    public string? State { get; set; }

    /// <summary>
    /// 发起授权时使用的回调地址，必须与 Provider 平台配置一致。
    /// </summary>
    [Required]
    [StringLength(512)]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// PKCE code verifier；启用 PKCE 的移动端 Provider 需要提交。
    /// </summary>
    [StringLength(256)]
    public string? CodeVerifier { get; set; }

    /// <summary>
    /// 客户端设备标识的哈希值，仅用于限流和审计关联，不保存原始设备标识。
    /// </summary>
    [StringLength(MobileAuthAuditLogConsts.MaxDeviceIdHashLength)]
    public string? DeviceIdHash { get; set; }
}
