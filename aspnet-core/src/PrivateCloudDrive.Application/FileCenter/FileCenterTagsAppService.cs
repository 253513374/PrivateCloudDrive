using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件标签和收藏应用服务。
/// 标签按当前用户隔离，文件与标签通过 FileNodeTag 关联；收藏直接保存在 FileNode 上。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Tags)]
public class FileCenterTagsAppService : FileCenterAppService, IFileCenterTagsAppService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<FileTag, Guid> _tagRepository;
    private readonly IRepository<FileNodeTag, Guid> _nodeTagRepository;
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="FileCenterTagsAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterTagsAppService(
        IGuidGenerator guidGenerator,
        IRepository<FileTag, Guid> tagRepository,
        IRepository<FileNodeTag, Guid> nodeTagRepository,
        IFileNodeRepository fileNodeRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _guidGenerator = guidGenerator;
        _tagRepository = tagRepository;
        _nodeTagRepository = nodeTagRepository;
        _fileNodeRepository = fileNodeRepository;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 获取当前用户的全部标签，按标准化名称排序。
    /// </summary>
    public virtual async Task<IReadOnlyList<FileTagDto>> GetListAsync()
    {
        var ownerId = GetOwnerId();
        var queryable = (await _tagRepository.GetQueryableAsync())
            .Where(tag => tag.TenantId == CurrentTenant.Id && tag.OwnerId == ownerId)
            .OrderBy(tag => tag.NormalizedName);

        return (await _asyncExecuter.ToListAsync(queryable))
            .Select(ToDto)
            .ToList();
    }

    /// <summary>
    /// 创建标签，并阻止同一用户下出现大小写不同但语义相同的重复标签。
    /// </summary>
    public virtual async Task<FileTagDto> CreateAsync(CreateFileTagInput input)
    {
        var ownerId = GetOwnerId();
        await EnsureTagNameAvailableAsync(ownerId, input.Name);

        var tag = new FileTag(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            ownerId,
            input.Name,
            input.Color);

        await _tagRepository.InsertAsync(tag, autoSave: true);

        return ToDto(tag);
    }

    /// <summary>
    /// 更新标签名称和颜色，保留已有文件关联关系。
    /// </summary>
    public virtual async Task<FileTagDto> UpdateAsync(Guid id, UpdateFileTagInput input)
    {
        var ownerId = GetOwnerId();
        var tag = await GetOwnerTagAsync(ownerId, id);
        await EnsureTagNameAvailableAsync(ownerId, input.Name, tag.Id);

        tag.Rename(input.Name);
        tag.SetColor(input.Color);

        await _tagRepository.UpdateAsync(tag, autoSave: true);

        return ToDto(tag);
    }

    /// <summary>
    /// 删除标签并清理该标签与所有文件节点的关联。
    /// </summary>
    public virtual async Task DeleteAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var tag = await GetOwnerTagAsync(ownerId, id);

        await _nodeTagRepository.DeleteDirectAsync(
            nodeTag =>
                nodeTag.TenantId == CurrentTenant.Id &&
                nodeTag.OwnerId == ownerId &&
                nodeTag.TagId == tag.Id);
        await _tagRepository.DeleteAsync(tag, autoSave: true);
    }

    /// <summary>
    /// 将标签添加到当前用户拥有的文件节点。重复添加会被幂等忽略。
    /// </summary>
    public virtual async Task AddToNodeAsync(Guid nodeId, Guid tagId)
    {
        var ownerId = GetOwnerId();
        var node = await GetOwnerNodeAsync(ownerId, nodeId);
        var tag = await GetOwnerTagAsync(ownerId, tagId);

        var existing = await _nodeTagRepository.FirstOrDefaultAsync(
            nodeTag =>
                nodeTag.TenantId == CurrentTenant.Id &&
                nodeTag.OwnerId == ownerId &&
                nodeTag.FileNodeId == node.Id &&
                nodeTag.TagId == tag.Id);

        if (existing != null)
        {
            return;
        }

        await _nodeTagRepository.InsertAsync(
            new FileNodeTag(
                _guidGenerator.Create(),
                CurrentTenant.Id,
                ownerId,
                node.Id,
                tag.Id),
            autoSave: true);
    }

    /// <summary>
    /// 从当前用户拥有的文件节点移除指定标签。
    /// </summary>
    public virtual async Task RemoveFromNodeAsync(Guid nodeId, Guid tagId)
    {
        var ownerId = GetOwnerId();
        await GetOwnerNodeAsync(ownerId, nodeId);
        await GetOwnerTagAsync(ownerId, tagId);

        await _nodeTagRepository.DeleteDirectAsync(
            nodeTag =>
                nodeTag.TenantId == CurrentTenant.Id &&
                nodeTag.OwnerId == ownerId &&
                nodeTag.FileNodeId == nodeId &&
                nodeTag.TagId == tagId);
    }

    /// <summary>
    /// 设置文件或文件夹收藏状态，用于媒体库和文件列表快速筛选。
    /// </summary>
    public virtual async Task<FileNodeDto> SetFavoriteAsync(Guid nodeId, SetFileFavoriteInput input)
    {
        var ownerId = GetOwnerId();
        var node = await GetOwnerNodeAsync(ownerId, nodeId);

        node.SetFavorite(input.IsFavorite);
        await _fileNodeRepository.UpdateAsync(node, autoSave: true);

        return ToNodeDto(node);
    }

    private async Task EnsureTagNameAvailableAsync(
        Guid ownerId,
        string name,
        Guid? currentTagId = null)
    {
        var normalizedName = FileTag.NormalizeName(name);
        var existing = await _tagRepository.FirstOrDefaultAsync(
            tag =>
                tag.TenantId == CurrentTenant.Id &&
                tag.OwnerId == ownerId &&
                tag.NormalizedName == normalizedName);

        if (existing != null && existing.Id != currentTagId)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterTagAlreadyExists)
                .WithData("Name", name);
        }
    }

    private async Task<FileTag> GetOwnerTagAsync(Guid ownerId, Guid id)
    {
        var tag = await _tagRepository.FirstOrDefaultAsync(
            item =>
                item.Id == id &&
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId);

        if (tag == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterTagNotFound)
                .WithData("Id", id);
        }

        return tag;
    }

    private async Task<FileNode> GetOwnerNodeAsync(Guid ownerId, Guid id)
    {
        var node = await _fileNodeRepository.FindByIdAsync(id, ownerId, CurrentTenant.Id);
        if (node == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", id);
        }

        return node;
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return CurrentUser.Id.Value;
    }

    private static FileTagDto ToDto(FileTag tag)
    {
        return new FileTagDto
        {
            Id = tag.Id,
            TenantId = tag.TenantId,
            OwnerId = tag.OwnerId,
            Name = tag.Name,
            NormalizedName = tag.NormalizedName,
            Color = tag.Color
        };
    }

    private static FileNodeDto ToNodeDto(FileNode node)
    {
        return new FileNodeDto
        {
            Id = node.Id,
            TenantId = node.TenantId,
            OwnerId = node.OwnerId,
            ParentId = node.ParentId,
            NodeType = node.NodeType,
            Name = node.Name,
            NormalizedName = node.NormalizedName,
            Size = node.Size,
            ContentType = node.ContentType,
            BlobName = node.BlobName,
            IsFavorite = node.IsFavorite,
            CreationTime = node.CreationTime,
            LastModificationTime = node.LastModificationTime
        };
    }
}
