using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 分享密码校验输入。密码只用于服务端即时比对，不应记录到日志。
/// </summary>
public class VerifySharePasswordInput
{
    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
