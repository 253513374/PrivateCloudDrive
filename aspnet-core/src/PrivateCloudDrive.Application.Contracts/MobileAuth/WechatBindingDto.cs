using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.MobileAuth;

public class WechatBindingDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid UserId { get; set; }

    public string AppId { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public DateTime CreationTime { get; set; }
}
