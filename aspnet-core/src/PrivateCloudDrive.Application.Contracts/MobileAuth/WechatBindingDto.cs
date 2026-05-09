using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示WechatBinding数据传输对象，用于跨层或 API 边界返回业务数据。
/// </summary>
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
