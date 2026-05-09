using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 当前用户第三方账号绑定的安全输出模型。
/// 只包含展示所需信息，不包含 ProviderUserId、授权码、Provider Token 或 Secret。
/// </summary>
public class ExternalBindingDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public DateTime CreationTime { get; set; }
}
