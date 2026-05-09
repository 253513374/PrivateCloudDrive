using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 用户媒体相册聚合。相册只保存媒体组织关系，不改变原始文件目录结构。
/// </summary>
public class MediaAlbum : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid? CoverFileNodeId { get; private set; }

    protected MediaAlbum()
    {
    }

    /// <summary>
    /// 初始化媒体相册。
    /// </summary>
    public MediaAlbum(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        [NotNull] string name,
        string? description = null)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        SetName(name);
        SetDescription(description);
    }

    /// <summary>
    /// 重命名相册。
    /// </summary>
    public void Rename(string name)
    {
        SetName(name);
    }

    /// <summary>
    /// 设置相册描述。
    /// </summary>
    public void SetDescription(string? description)
    {
        Description = Check.Length(description, nameof(description), MediaAlbumConsts.MaxDescriptionLength);
    }

    /// <summary>
    /// 设置相册封面。
    /// </summary>
    public void SetCover(Guid? fileNodeId)
    {
        CoverFileNodeId = fileNodeId;
    }

    /// <summary>
    /// 标准化相册名称。
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
        Name = Check.Length(trimmedName, nameof(name), MediaAlbumConsts.MaxNameLength)!;
        NormalizedName = Check.Length(
            NormalizeName(Name),
            nameof(name),
            MediaAlbumConsts.MaxNormalizedNameLength)!;
    }
}
