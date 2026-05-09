using System;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心的核心节点聚合，统一表示文件夹和文件。
/// 同一租户、同一用户、同一父目录下通过 NormalizedName 保证名称唯一；删除采用 ABP 软删除以支持回收站恢复。
/// </summary>
public class FileNode : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid? ParentId { get; private set; }

    public FileNodeType NodeType { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public long Size { get; private set; }

    public string? ContentType { get; private set; }

    public string? BlobName { get; private set; }

    public bool IsFavorite { get; private set; }

    protected FileNode()
    {
    }

    private FileNode(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        FileNodeType nodeType,
        [NotNull] string name,
        long size = 0,
        string? contentType = null,
        string? blobName = null)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        ParentId = parentId;
        NodeType = nodeType;

        SetName(name);
        SetFileMetadata(size, contentType, blobName);
    }

    /// <summary>
    /// 创建文件夹节点。文件夹不允许携带文件大小和 Blob 引用。
    /// </summary>
    public static FileNode CreateFolder(Guid id, Guid? tenantId, Guid ownerId, Guid? parentId, string name)
    {
        return new FileNode(id, tenantId, ownerId, parentId, FileNodeType.Folder, name);
    }

    /// <summary>
    /// 创建文件节点，并绑定底层 Blob 对象名称和内容类型。
    /// </summary>
    public static FileNode CreateFile(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string name,
        long size,
        string? contentType = null,
        string? blobName = null)
    {
        return new FileNode(id, tenantId, ownerId, parentId, FileNodeType.File, name, size, contentType, blobName);
    }

    /// <summary>
    /// 重命名节点。调用方需先完成同目录重名校验。
    /// </summary>
    public void Rename(string name)
    {
        SetName(name);
    }

    /// <summary>
    /// 移动节点到目标父目录；null 表示移动到根目录。
    /// 调用方需先校验目标目录存在、不能移动到自身或子孙目录、目标目录不重名。
    /// </summary>
    public void MoveTo(Guid? parentId)
    {
        ParentId = parentId;
    }

    /// <summary>
    /// 从回收站恢复节点，清除软删除标记。
    /// 子节点递归恢复由领域服务统一处理。
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeleterId = null;
        DeletionTime = null;
    }

    /// <summary>
    /// 设置收藏状态，用于客户端快速筛选常用文件。
    /// </summary>
    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
    }

    /// <summary>
    /// 更新文件元数据。文件夹大小必须保持为 0，避免把目录误当作可下载文件。
    /// </summary>
    public void SetFileMetadata(long size, string? contentType = null, string? blobName = null)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "File size cannot be negative.");
        }

        if (NodeType == FileNodeType.Folder && size != 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterFolderCannotHaveSize);
        }

        Size = size;
        ContentType = Check.Length(contentType, nameof(contentType), FileNodeConsts.MaxContentTypeLength);
        BlobName = Check.Length(blobName, nameof(blobName), FileNodeConsts.MaxBlobNameLength);
    }

    /// <summary>
    /// 标准化节点名称，用于大小写不敏感的同目录重名校验。
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

        Name = Check.Length(
            trimmedName,
            nameof(name),
            FileNodeConsts.MaxNameLength)!;

        NormalizedName = Check.Length(
            NormalizeName(Name),
            nameof(name),
            FileNodeConsts.MaxNormalizedNameLength)!;
    }
}
