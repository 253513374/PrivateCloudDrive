using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体库查询应用服务。
/// 基于文件节点的内容类型和扩展名聚合图片、视频列表，并支持收藏和标签筛选。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterMediaLibraryAppService : FileCenterAppService, IFileCenterMediaLibraryAppService
{
    private readonly IRepository<FileNode, Guid> _fileNodeRepository;
    private readonly IRepository<FileNodeTag, Guid> _nodeTagRepository;
    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly IRepository<MediaAlbum, Guid> _mediaAlbumRepository;
    private readonly IRepository<MediaAlbumItem, Guid> _mediaAlbumItemRepository;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IFileCenterMediaAssetService _mediaAssetService;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="FileCenterMediaLibraryAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterMediaLibraryAppService(
        IRepository<FileNode, Guid> fileNodeRepository,
        IRepository<FileNodeTag, Guid> nodeTagRepository,
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        IRepository<MediaAlbum, Guid> mediaAlbumRepository,
        IRepository<MediaAlbumItem, Guid> mediaAlbumItemRepository,
        IBackgroundJobManager backgroundJobManager,
        IFileCenterMediaAssetService mediaAssetService,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _fileNodeRepository = fileNodeRepository;
        _nodeTagRepository = nodeTagRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _mediaAlbumRepository = mediaAlbumRepository;
        _mediaAlbumItemRepository = mediaAlbumItemRepository;
        _backgroundJobManager = backgroundJobManager;
        _mediaAssetService = mediaAssetService;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 查询当前用户图片文件列表。
    /// </summary>
    public virtual Task<PagedResultDto<FileNodeDto>> GetImagesAsync(GetMediaFilesInput input)
    {
        return GetMediaFilesAsync(input, isImage: true);
    }

    /// <summary>
    /// 查询当前用户视频文件列表。
    /// </summary>
    public virtual Task<PagedResultDto<FileNodeDto>> GetVideosAsync(GetMediaFilesInput input)
    {
        return GetMediaFilesAsync(input, isImage: false);
    }

    /// <summary>
    /// 查询图片和视频混合时间线，返回扁平分页列表，由客户端进行月份或日期分组。
    /// </summary>
    public virtual async Task<PagedResultDto<MediaTimelineItemDto>> GetTimelineAsync(GetMediaTimelineInput input)
    {
        var items = await GetTimelineItemsAsync(input);
        var totalCount = items.Count;

        return new PagedResultDto<MediaTimelineItemDto>(
            totalCount,
            items
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList());
    }

    /// <summary>
    /// 查询单个媒体文件的详情和处理状态。
    /// </summary>
    public virtual async Task<MediaDetailDto> GetDetailAsync(Guid fileNodeId)
    {
        var ownerId = GetOwnerId();
        var node = await GetOwnerMediaNodeAsync(ownerId, fileNodeId);
        var asset = await GetMediaAssetAsync(ownerId, fileNodeId);

        return FileCenterMediaLibraryHelpers.ToDetail(node, asset);
    }

    /// <summary>
    /// 查询媒体处理状态列表。
    /// </summary>
    public virtual async Task<PagedResultDto<MediaTimelineItemDto>> GetProcessingStatusAsync(GetMediaProcessingStatusInput input)
    {
        var timelineInput = new GetMediaTimelineInput
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            MediaType = input.MediaType,
            ProcessStatus = input.Status
        };

        var items = await GetTimelineItemsAsync(timelineInput);

        if (!input.Status.HasValue)
        {
            items = items
                .Where(item => item.ProcessStatus != MediaAssetProcessStatus.Completed)
                .ToList();
        }

        return new PagedResultDto<MediaTimelineItemDto>(
            items.Count,
            items
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList());
    }

    /// <summary>
    /// 重新投递失败或待处理媒体的处理任务。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<MediaDetailDto> RetryProcessingAsync(Guid fileNodeId)
    {
        var ownerId = GetOwnerId();
        var node = await GetOwnerMediaNodeAsync(ownerId, fileNodeId);
        var asset = await GetMediaAssetAsync(ownerId, fileNodeId)
                    ?? await _mediaAssetService.CreatePendingAssetAsync(node);

        if (asset == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyMediaFileCanBeManaged)
                .WithData("Id", fileNodeId);
        }

        if (asset.ProcessStatus is not (MediaAssetProcessStatus.Pending or MediaAssetProcessStatus.Failed))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterMediaAssetCannotRetry)
                .WithData("Id", fileNodeId)
                .WithData("ProcessStatus", asset.ProcessStatus);
        }

        asset.MarkProcessing();
        await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);

        await _backgroundJobManager.EnqueueAsync(
            new MediaAssetProcessingJobArgs
            {
                MediaAssetId = asset.Id,
                FileNodeId = node.Id
            });

        return FileCenterMediaLibraryHelpers.ToDetail(node, asset);
    }

    /// <summary>
    /// 统一媒体查询入口，按媒体类型、收藏状态和标签关联组合筛选。
    /// </summary>
    private async Task<PagedResultDto<FileNodeDto>> GetMediaFilesAsync(GetMediaFilesInput input, bool isImage)
    {
        var ownerId = GetOwnerId();
        var queryable = (await _fileNodeRepository.GetQueryableAsync())
            .Where(node =>
                node.TenantId == CurrentTenant.Id &&
                node.OwnerId == ownerId &&
                node.NodeType == FileNodeType.File);

        queryable = isImage
            ? ApplyImageFilter(queryable)
            : ApplyVideoFilter(queryable);

        if (input.IsFavorite.HasValue)
        {
            queryable = queryable.Where(node => node.IsFavorite == input.IsFavorite.Value);
        }

        if (input.TagId.HasValue)
        {
            var nodeTags = await _nodeTagRepository.GetQueryableAsync();
            var taggedNodeIds = nodeTags
                .Where(nodeTag =>
                    nodeTag.TenantId == CurrentTenant.Id &&
                    nodeTag.OwnerId == ownerId &&
                    nodeTag.TagId == input.TagId.Value)
                .Select(nodeTag => nodeTag.FileNodeId);

            queryable = queryable.Where(node => taggedNodeIds.Contains(node.Id));
        }

        var totalCount = await _asyncExecuter.LongCountAsync(queryable);
        var items = await _asyncExecuter.ToListAsync(
            queryable
                .OrderByDescending(node => node.LastModificationTime ?? node.CreationTime)
                .ThenBy(node => node.NormalizedName)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<FileNodeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    private async Task<List<MediaTimelineItemDto>> GetTimelineItemsAsync(GetMediaTimelineInput input)
    {
        var ownerId = GetOwnerId();
        var queryable = (await _fileNodeRepository.GetQueryableAsync())
            .Where(node =>
                node.TenantId == CurrentTenant.Id &&
                node.OwnerId == ownerId &&
                node.NodeType == FileNodeType.File);

        if (input.IsFavorite.HasValue)
        {
            queryable = queryable.Where(node => node.IsFavorite == input.IsFavorite.Value);
        }

        if (input.TagId.HasValue)
        {
            var nodeTags = await _nodeTagRepository.GetQueryableAsync();
            var taggedNodeIds = nodeTags
                .Where(nodeTag =>
                    nodeTag.TenantId == CurrentTenant.Id &&
                    nodeTag.OwnerId == ownerId &&
                    nodeTag.TagId == input.TagId.Value)
                .Select(nodeTag => nodeTag.FileNodeId);

            queryable = queryable.Where(node => taggedNodeIds.Contains(node.Id));
        }

        if (input.AlbumId.HasValue)
        {
            await GetOwnerAlbumAsync(ownerId, input.AlbumId.Value);
            var albumItems = await _mediaAlbumItemRepository.GetQueryableAsync();
            var albumNodeIds = albumItems
                .Where(item =>
                    item.TenantId == CurrentTenant.Id &&
                    item.OwnerId == ownerId &&
                    item.AlbumId == input.AlbumId.Value)
                .Select(item => item.FileNodeId);

            queryable = queryable.Where(node => albumNodeIds.Contains(node.Id));
        }

        var nodes = (await _asyncExecuter.ToListAsync(queryable))
            .Where(node => FileCenterMediaLibraryHelpers.IsMediaNode(node, input.MediaType))
            .ToList();
        if (nodes.Count == 0)
        {
            return [];
        }

        var nodeIds = nodes.Select(node => node.Id).ToList();
        var assets = await _mediaAssetRepository.GetListAsync(
            asset =>
                asset.TenantId == CurrentTenant.Id &&
                asset.OwnerId == ownerId &&
                nodeIds.Contains(asset.FileNodeId));
        var assetByNodeId = assets.ToDictionary(asset => asset.FileNodeId, asset => asset);

        var items = nodes
            .Select(node => FileCenterMediaLibraryHelpers.ToTimelineItem(
                node,
                assetByNodeId.GetValueOrDefault(node.Id)))
            .Where(item => !input.ProcessStatus.HasValue || item.ProcessStatus == input.ProcessStatus.Value)
            .Where(item => !input.StartTime.HasValue || item.TimelineTime >= input.StartTime.Value)
            .Where(item => !input.EndTime.HasValue || item.TimelineTime <= input.EndTime.Value)
            .OrderByDescending(item => item.TimelineTime)
            .ThenBy(item => item.Name)
            .ToList();

        return items;
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

    private Task<MediaAsset?> GetMediaAssetAsync(Guid ownerId, Guid fileNodeId)
    {
        return _mediaAssetRepository.FirstOrDefaultAsync(
            asset =>
                asset.TenantId == CurrentTenant.Id &&
                asset.OwnerId == ownerId &&
                asset.FileNodeId == fileNodeId);
    }

    private async Task<MediaAlbum> GetOwnerAlbumAsync(Guid ownerId, Guid albumId)
    {
        var album = await _mediaAlbumRepository.FirstOrDefaultAsync(
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

    private static IQueryable<FileNode> ApplyImageFilter(IQueryable<FileNode> queryable)
    {
        return queryable.Where(node =>
            (node.ContentType != null && node.ContentType.StartsWith("image/")) ||
            node.NormalizedName.EndsWith(".jpg") ||
            node.NormalizedName.EndsWith(".jpeg") ||
            node.NormalizedName.EndsWith(".png") ||
            node.NormalizedName.EndsWith(".gif") ||
            node.NormalizedName.EndsWith(".webp") ||
            node.NormalizedName.EndsWith(".heic") ||
            node.NormalizedName.EndsWith(".heif"));
    }

    private static IQueryable<FileNode> ApplyVideoFilter(IQueryable<FileNode> queryable)
    {
        return queryable.Where(node =>
            (node.ContentType != null && node.ContentType.StartsWith("video/")) ||
            node.NormalizedName.EndsWith(".mp4") ||
            node.NormalizedName.EndsWith(".m4v") ||
            node.NormalizedName.EndsWith(".mov") ||
            node.NormalizedName.EndsWith(".mkv") ||
            node.NormalizedName.EndsWith(".webm") ||
            node.NormalizedName.EndsWith(".avi"));
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return CurrentUser.Id.Value;
    }

    private static FileNodeDto ToDto(FileNode node)
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
