using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 用户自定义文件标签聚合。
/// 标签按租户和所有者隔离，NormalizedName 用于大小写不敏感的重名校验。
/// </summary>
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

    /// <summary>
    /// 创建标签，并标准化名称与颜色值。
    /// </summary>
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

    /// <summary>
    /// 重命名标签。调用方需先校验同一用户下名称不重复。
    /// </summary>
    public void Rename(string name)
    {
        SetName(name);
    }

    /// <summary>
    /// 设置标签颜色，通常为客户端可直接使用的颜色字符串。
    /// </summary>
    public void SetColor(string? color)
    {
        Color = Check.Length(color, nameof(color), FileTagConsts.MaxColorLength);
    }

    /// <summary>
    /// 标准化标签名称，用于大小写不敏感的唯一性判断。
    /// </summary>
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
