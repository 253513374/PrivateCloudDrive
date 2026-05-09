using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体相册应用服务。
/// 相册只管理图片/视频文件的组织关系，不改变文件目录结构，也不删除原始文件。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterMediaAlbumsAppService : FileCenterAppService, IFileCenterMediaAlbumsAppService
{
    private const int MaxBatchItemCount = 100;

    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<MediaAlbum, Guid> _albumRepository;
    private readonly IRepository<MediaAlbumItem, Guid> _albumItemRepository;
    private readonly IRepository<FileNode, Guid> _fileNodeRepository;
    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="FileCenterMediaAlbumsAppService"/> 的新实例。
    /// </summary>
    public FileCenterMediaAlbumsAppService(
        IGuidGenerator guidGenerator,
        IRepository<MediaAlbum, Guid> albumRepository,
        IRepository<MediaAlbumItem, Guid> albumItemRepository,
        IRepository<FileNode, Guid> fileNodeRepository,
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _guidGenerator = guidGenerator;
        _albumRepository = albumRepository;
        _albumItemRepository = albumItemRepository;
        _fileNodeRepository = fileNodeRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 查询当前用户的相册列表。
    /// </summary>
    public virtual async Task<PagedResultDto<MediaAlbumDto>> GetListAsync(PagedResultRequestDto input)
    {
        var ownerId = GetOwnerId();
        var queryable = (await _albumRepository.GetQueryableAsync())
            .Where(album => album.TenantId == CurrentTenant.Id && album.OwnerId == ownerId)
            .OrderByDescending(album => album.LastModificationTime ?? album.CreationTime)
            .ThenBy(album => album.NormalizedName);

        var totalCount = await _asyncExecuter.LongCountAsync(queryable);
        var albums = await _asyncExecuter.ToListAsync(
            queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        var items = new List<MediaAlbumDto>();
        foreach (var album in albums)
        {
            items.Add(await ToDtoAsync(ownerId, album));
        }

        return new PagedResultDto<MediaAlbumDto>(totalCount, items);
    }

    /// <summary>
    /// 获取单个相册。
    /// </summary>
    public virtual async Task<MediaAlbumDto> GetAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var album = await GetOwnerAlbumAsync(ownerId, id);

        return await ToDtoAsync(ownerId, album);
    }

    /// <summary>
    /// 创建媒体相册。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<MediaAlbumDto> CreateAsync(CreateMediaAlbumInput input)
    {
        var ownerId = GetOwnerId();
        await EnsureAlbumNameAvailableAsync(ownerId, input.Name);

        var album = new MediaAlbum(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            ownerId,
            input.Name,
            input.Description);

        await _albumRepository.InsertAsync(album, autoSave: true);

        return await ToDtoAsync(ownerId, album);
    }

    /// <summary>
    /// 更新媒体相册。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<MediaAlbumDto> UpdateAsync(Guid id, UpdateMediaAlbumInput input)
    {
        var ownerId = GetOwnerId();
        var album = await GetOwnerAlbumAsync(ownerId, id);
        await EnsureAlbumNameAvailableAsync(ownerId, input.Name, album.Id);

        album.Rename(input.Name);
        album.SetDescription(input.Description);

        await _albumRepository.UpdateAsync(album, autoSave: true);

        return await ToDtoAsync(ownerId, album);
    }

    /// <summary>
    /// 删除媒体相册；只删除相册关系，不删除原文件。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var album = await GetOwnerAlbumAsync(ownerId, id);

        await _albumItemRepository.DeleteDirectAsync(
            item =>
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId &&
                item.AlbumId == album.Id);
        await _albumRepository.DeleteAsync(album, autoSave: true);
    }

    /// <summary>
    /// 查询相册中的媒体项目。
    /// </summary>
    public virtual async Task<PagedResultDto<MediaTimelineItemDto>> GetItemsAsync(Guid id, PagedResultRequestDto input)
    {
        var ownerId = GetOwnerId();
        await GetOwnerAlbumAsync(ownerId, id);

        var albumItems = await GetAlbumItemsAsync(ownerId, id);
        var totalCount = albumItems.Count;
        var selectedItems = albumItems
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<MediaTimelineItemDto>(
            totalCount,
            await ToTimelineItemsAsync(ownerId, selectedItems.Select(item => item.FileNodeId).ToList()));
    }

    /// <summary>
    /// 批量添加媒体到相册。重复添加会被幂等忽略。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<IReadOnlyList<MediaTimelineItemDto>> AddItemsAsync(Guid id, AddMediaAlbumItemsInput input)
    {
        var ownerId = GetOwnerId();
        var album = await GetOwnerAlbumAsync(ownerId, id);
        var fileNodeIds = NormalizeBatchIds(input.FileNodeIds);
        var addedNodeIds = new List<Guid>();

        foreach (var fileNodeId in fileNodeIds)
        {
            var node = await GetOwnerMediaNodeAsync(ownerId, fileNodeId);
            var existing = await _albumItemRepository.FirstOrDefaultAsync(
                item =>
                    item.TenantId == CurrentTenant.Id &&
                    item.OwnerId == ownerId &&
                    item.AlbumId == album.Id &&
                    item.FileNodeId == node.Id);

            if (existing != null)
            {
                continue;
            }

            await _albumItemRepository.InsertAsync(
                new MediaAlbumItem(
                    _guidGenerator.Create(),
                    CurrentTenant.Id,
                    ownerId,
                    album.Id,
                    node.Id),
                autoSave: false);
            addedNodeIds.Add(node.Id);

            if (!album.CoverFileNodeId.HasValue)
            {
                album.SetCover(node.Id);
            }
        }

        await _albumRepository.UpdateAsync(album, autoSave: true);

        return await ToTimelineItemsAsync(ownerId, addedNodeIds);
    }

    /// <summary>
    /// 从相册移除媒体，不删除原文件。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task RemoveItemAsync(Guid id, Guid fileNodeId)
    {
        var ownerId = GetOwnerId();
        var album = await GetOwnerAlbumAsync(ownerId, id);
        var item = await _albumItemRepository.FirstOrDefaultAsync(
            albumItem =>
                albumItem.TenantId == CurrentTenant.Id &&
                albumItem.OwnerId == ownerId &&
                albumItem.AlbumId == album.Id &&
                albumItem.FileNodeId == fileNodeId);

        if (item == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumItemNotFound)
                .WithData("AlbumId", id)
                .WithData("FileNodeId", fileNodeId);
        }

        await _albumItemRepository.DeleteAsync(item, autoSave: true);

        if (album.CoverFileNodeId == fileNodeId)
        {
            album.SetCover(null);
            await _albumRepository.UpdateAsync(album, autoSave: true);
        }
    }

    /// <summary>
    /// 设置相册封面，封面必须来自该相册内的媒体。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<MediaAlbumDto> SetCoverAsync(Guid id, SetMediaAlbumCoverInput input)
    {
        var ownerId = GetOwnerId();
        var album = await GetOwnerAlbumAsync(ownerId, id);
        await GetOwnerMediaNodeAsync(ownerId, input.FileNodeId);

        var item = await _albumItemRepository.FirstOrDefaultAsync(
            albumItem =>
                albumItem.TenantId == CurrentTenant.Id &&
                albumItem.OwnerId == ownerId &&
                albumItem.AlbumId == album.Id &&
                albumItem.FileNodeId == input.FileNodeId);

        if (item == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumItemNotFound)
                .WithData("AlbumId", id)
                .WithData("FileNodeId", input.FileNodeId);
        }

        album.SetCover(input.FileNodeId);
        await _albumRepository.UpdateAsync(album, autoSave: true);

        return await ToDtoAsync(ownerId, album);
    }

    private async Task EnsureAlbumNameAvailableAsync(
        Guid ownerId,
        string name,
        Guid? currentAlbumId = null)
    {
        var normalizedName = MediaAlbum.NormalizeName(name);
        var existing = await _albumRepository.FirstOrDefaultAsync(
            album =>
                album.TenantId == CurrentTenant.Id &&
                album.OwnerId == ownerId &&
                album.NormalizedName == normalizedName);

        if (existing != null && existing.Id != currentAlbumId)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumAlreadyExists)
                .WithData("Name", name);
        }
    }

    private async Task<MediaAlbum> GetOwnerAlbumAsync(Guid ownerId, Guid albumId)
    {
        var album = await _albumRepository.FirstOrDefaultAsync(
            item =>
                item.Id == albumId &&
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId);

        if (album == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAlbumNotFound)
                .WithData("Id", albumId);
        }

        return album;
    }

    private async Task<FileNode> GetOwnerMediaNodeAsync(Guid ownerId, Guid fileNodeId)
    {
        var node = await _fileNodeRepository.FirstOrDefaultAsync(
            item =>
                item.Id == fileNodeId &&
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId &&
                item.NodeType == FileNodeType.File);

        if (node == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", fileNodeId);
        }

        if (!FileCenterMediaLibraryHelpers.IsMediaNode(node))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyMediaFileCanBeManaged)
                .WithData("Id", fileNodeId);
        }

        return node;
    }

    private async Task<List<MediaAlbumItem>> GetAlbumItemsAsync(Guid ownerId, Guid albumId)
    {
        var queryable = (await _albumItemRepository.GetQueryableAsync())
            .Where(item =>
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId &&
                item.AlbumId == albumId)
            .OrderBy(item => item.SortOrder)
            .ThenByDescending(item => item.CreationTime);

        return await _asyncExecuter.ToListAsync(queryable);
    }

    private async Task<IReadOnlyList<MediaTimelineItemDto>> ToTimelineItemsAsync(
        Guid ownerId,
        IReadOnlyCollection<Guid> fileNodeIds)
    {
        if (fileNodeIds.Count == 0)
        {
            return [];
        }

        var nodes = await _fileNodeRepository.GetListAsync(
            node =>
                node.TenantId == CurrentTenant.Id &&
                node.OwnerId == ownerId &&
                fileNodeIds.Contains(node.Id));
        var assets = await _mediaAssetRepository.GetListAsync(
            asset =>
                asset.TenantId == CurrentTenant.Id &&
                asset.OwnerId == ownerId &&
                fileNodeIds.Contains(asset.FileNodeId));
        var assetByNodeId = assets.ToDictionary(asset => asset.FileNodeId, asset => asset);

        return nodes
            .Where(node => FileCenterMediaLibraryHelpers.IsMediaNode(node))
            .Select(node => FileCenterMediaLibraryHelpers.ToTimelineItem(
                node,
                assetByNodeId.GetValueOrDefault(node.Id)))
            .OrderByDescending(item => item.TimelineTime)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private async Task<MediaAlbumDto> ToDtoAsync(Guid ownerId, MediaAlbum album)
    {
        var itemCount = await _albumItemRepository.CountAsync(
            item =>
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId &&
                item.AlbumId == album.Id);
        Guid? coverThumbnailBlobObjectId = null;

        if (album.CoverFileNodeId.HasValue)
        {
            var coverAsset = await _mediaAssetRepository.FirstOrDefaultAsync(
                asset =>
                    asset.TenantId == CurrentTenant.Id &&
                    asset.OwnerId == ownerId &&
                    asset.FileNodeId == album.CoverFileNodeId.Value);

            coverThumbnailBlobObjectId = coverAsset?.ThumbnailBlobObjectId;
        }

        return new MediaAlbumDto
        {
            Id = album.Id,
            Name = album.Name,
            Description = album.Description,
            CoverFileNodeId = album.CoverFileNodeId,
            CoverThumbnailBlobObjectId = coverThumbnailBlobObjectId,
            ItemsCount = (int)itemCount,
            CreationTime = album.CreationTime,
            LastModificationTime = album.LastModificationTime
        };
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for media album operations.");
        }

        return CurrentUser.Id.Value;
    }

    private static IReadOnlyList<Guid> NormalizeBatchIds(IReadOnlyCollection<Guid>? ids)
    {
        var normalizedIds = ids?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (normalizedIds.Count == 0 || normalizedIds.Count > MaxBatchItemCount)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterBatchSelectionInvalid)
                .WithData("MaxCount", MaxBatchItemCount);
        }

        return normalizedIds;
    }
}
