using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 登录用户视角的分享 DTO，包含分享管理所需的状态和权限信息。
/// </summary>
public class FileShareDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public Guid FileNodeId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public FileNodeType NodeType { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime? ExpirationTime { get; set; }

    public DateTime CreationTime { get; set; }

    public bool AllowDownload { get; set; }

    public bool RequiresPassword { get; set; }

    public int VisitCount { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsExpired { get; set; }
}
