using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

public class FileTag : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Color { get; private set; }

    protected FileTag()
    {
    }

    public FileTag(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        [NotNull] string name,
        string? color = null)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        SetName(name);
        SetColor(color);
    }

    public void Rename(string name)
    {
        SetName(name);
    }

    public void SetColor(string? color)
    {
        Color = Check.Length(color, nameof(color), FileTagConsts.MaxColorLength);
    }

    public static string NormalizeName(string name)
    {
        return Check.NotNullOrWhiteSpace(name, nameof(name))
            .Trim()
            .ToUpperInvariant();
    }

    private void SetName(string name)
    {
        var trimmedName = Check.NotNullOrWhiteSpace(name, nameof(name)).Trim();

        Name = Check.Length(trimmedName, nameof(name), FileTagConsts.MaxNameLength)!;
        NormalizedName = Check.Length(
            NormalizeName(Name),
            nameof(name),
            FileTagConsts.MaxNormalizedNameLength)!;
    }
}
