using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件分享聚合，保存分享令牌、过期时间、下载权限和可选密码哈希。
/// 分享访问以 token 为入口，业务上应避免暴露原始文件路径或 Blob 名称。
/// </summary>
public class FileShare : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid FileNodeId { get; private set; }

    public string Token { get; private set; } = null!;

    public string? PasswordSalt { get; private set; }

    public string? PasswordHash { get; private set; }

    public DateTime? ExpirationTime { get; private set; }

    public bool AllowDownload { get; private set; }

    public int VisitCount { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool RequiresPassword => !string.IsNullOrWhiteSpace(PasswordHash);

    protected FileShare()
    {
    }

    /// <summary>
    /// 创建分享。密码只保存盐和哈希，不保存明文密码。
    /// </summary>
    public FileShare(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid fileNodeId,
        [NotNull] string token,
        DateTime? expirationTime,
        bool allowDownload,
        string? passwordSalt = null,
        string? passwordHash = null)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        FileNodeId = fileNodeId;
        Token = Check.Length(
            Check.NotNullOrWhiteSpace(token, nameof(token)),
            nameof(token),
            FileShareConsts.MaxTokenLength)!;
        ExpirationTime = expirationTime;
        AllowDownload = allowDownload;
        PasswordSalt = Check.Length(passwordSalt, nameof(passwordSalt), FileShareConsts.MaxPasswordSaltLength);
        PasswordHash = Check.Length(passwordHash, nameof(passwordHash), FileShareConsts.MaxPasswordHashLength);
        IsEnabled = true;
    }

    /// <summary>
    /// 校验分享是否仍可访问；禁用或过期的分享统一阻止访问。
    /// </summary>
    public void EnsureAccessible(DateTime now)
    {
        if (!IsEnabled)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterShareNotFound);
        }

        if (ExpirationTime.HasValue && ExpirationTime.Value <= now)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterShareExpired);
        }
    }

    /// <summary>
    /// 记录分享访问次数，用于分享管理和后续审计分析。
    /// </summary>
    public void IncreaseVisitCount()
    {
        VisitCount++;
    }

    /// <summary>
    /// 禁用分享链接。禁用后 token 保留但不再允许公开访问。
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }
}
