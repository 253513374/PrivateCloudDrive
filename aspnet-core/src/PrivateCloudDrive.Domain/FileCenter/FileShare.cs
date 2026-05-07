using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

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

    public void IncreaseVisitCount()
    {
        VisitCount++;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}
