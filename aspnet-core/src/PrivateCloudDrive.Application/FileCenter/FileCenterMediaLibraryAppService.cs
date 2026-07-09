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
    private readonly IRepository<FileCenterOperationLog, Guid> _fileCenterOperationLogRepository;

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
        IAsyncQueryableExecuter asyncExecuter,
        IRepository<FileCenterOperationLog, Guid> fileCenterOperationLogRepository)
    {
        _fileNodeRepository = fileNodeRepository;
        _nodeTagRepository = nodeTagRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _mediaAlbumRepository = mediaAlbumRepository;
        _mediaAlbumItemRepository = mediaAlbumItemRepository;
        _backgroundJobManager = backgroundJobManager;
        _mediaAssetService = mediaAssetService;
        _asyncExecuter = asyncExecuter;
        _fileCenterOperationLogRepository = fileCenterOperationLogRepository;
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
    /// 所有过滤、排序和分页均在数据库侧完成，避免全量内存排序风险。
    /// </summary>
    public virtual async Task<PagedResultDto<MediaTimelineItemDto>> GetTimelineAsync(GetMediaTimelineInput input)
    {
        return await GetTimelineItemsPagedAsync(input, excludeCompleted: false);
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
    /// 所有过滤、排序和分页均在数据库侧完成。
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

        // When no specific status filter is given, default to showing non-Completed items
        if (!input.Status.HasValue)
        {
            var result = await GetTimelineItemsPagedAsync(timelineInput, excludeCompleted: true);
            return result;
        }

        return await GetTimelineItemsPagedAsync(timelineInput, excludeCompleted: false);
    }

    /// <summary>
    /// 重新投递失败或待处理媒体的处理任务。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<MediaDetailDto> RetryProcessingAsync(Guid fileNodeId)
    {
        var ownerId = GetOwnerId();
        var node = await GetOwnerMediaNodeAsync(ownerId, fileNodeId);
        var existingAsset = await GetMediaAssetAsync(ownerId, fileNodeId);
        var shouldEnqueueJob = existingAsset != null;

        var asset = existingAsset ?? await _mediaAssetService.CreatePendingAssetAsync(node);

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

        var statusBefore = asset.ProcessStatus;

        asset.MarkProcessing();
        await _mediaAssetRepository.UpdateAsync(asset, autoSave: true);

        if (shouldEnqueueJob)
        {
            await _backgroundJobManager.EnqueueAsync(
                new MediaAssetProcessingJobArgs
                {
                    MediaAssetId = asset.Id,
                    FileNodeId = node.Id
                });
        }

        await _fileCenterOperationLogRepository.InsertAsync(
            new FileCenterOperationLog(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                node.Id,
                asset.Id,
                FileCenterOperationLogConsts.ActionMediaRetry,
                statusBefore.ToString(),
                asset.ProcessStatus.ToString(),
                CurrentUser.Id));

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

    private const int FallbackMaxResultCount = 50;

    /// <summary>
    /// 统一时间线查询入口，分两路从数据库分页查询：
    /// 1. 主路径：从 MediaAsset 查询，将 MediaType/ProcessStatus/TakenAt 过滤下推到数据库
    /// 2. 兜底路径：从 FileNode 查询无 MediaAsset 的媒体文件（待处理状态）
    /// 两路结果在内存合并后按 TimelineTime 倒序排列并截取最终分页窗口。
    /// </summary>
    private async Task<PagedResultDto<MediaTimelineItemDto>> GetTimelineItemsPagedAsync(
        GetMediaTimelineInput input, bool excludeCompleted)
    {
        var ownerId = GetOwnerId();
        var maxResult = Math.Min(
            input.MaxResultCount > 0 ? input.MaxResultCount : 10,
            GetMediaTimelineInput.MaxAllowedResultCount);

        // --- 主路径：MediaAsset 查询下推 ---
        var assetQuery = await _mediaAssetRepository.GetQueryableAsync();
        assetQuery = assetQuery.Where(a =>
            a.TenantId == CurrentTenant.Id &&
            a.OwnerId == ownerId);

        if (input.MediaType.HasValue)
        {
            assetQuery = assetQuery.Where(a => a.MediaType == input.MediaType.Value);
        }

        if (input.ProcessStatus.HasValue)
        {
            assetQuery = assetQuery.Where(a => a.ProcessStatus == input.ProcessStatus.Value);
        }
        else if (excludeCompleted)
        {
            assetQuery = assetQuery.Where(a => a.ProcessStatus != MediaAssetProcessStatus.Completed);
        }

        if (input.StartTime.HasValue)
        {
            assetQuery = assetQuery.Where(a => a.TakenAt >= input.StartTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            assetQuery = assetQuery.Where(a => a.TakenAt <= input.EndTime.Value);
        }

        // Join with FileNode for node-level filters and ordering
        var fileNodeQueryable = await _fileNodeRepository.GetQueryableAsync();

        var assetJoinedQuery = from asset in assetQuery
                               join node in fileNodeQueryable
                                   on asset.FileNodeId equals node.Id
                               where node.TenantId == CurrentTenant.Id
                                  && node.OwnerId == ownerId
                                  && node.NodeType == FileNodeType.File
                               select new { Node = node, Asset = asset };

        // Album filter
        if (input.AlbumId.HasValue)
        {
            await GetOwnerAlbumAsync(ownerId, input.AlbumId.Value);
            var albumItemsQueryable = await _mediaAlbumItemRepository.GetQueryableAsync();
            var albumNodeIds = albumItemsQueryable
                .Where(item =>
                    item.TenantId == CurrentTenant.Id &&
                    item.OwnerId == ownerId &&
                    item.AlbumId == input.AlbumId.Value)
                .Select(item => item.FileNodeId);

            assetJoinedQuery = assetJoinedQuery.Where(x => albumNodeIds.Contains(x.Node.Id));
        }

        // IsFavorite filter
        if (input.IsFavorite.HasValue)
        {
            assetJoinedQuery = assetJoinedQuery.Where(x => x.Node.IsFavorite == input.IsFavorite.Value);
        }

        // TagId filter
        if (input.TagId.HasValue)
        {
            var nodeTagsQueryable = await _nodeTagRepository.GetQueryableAsync();
            var taggedNodeIds = nodeTagsQueryable
                .Where(nodeTag =>
                    nodeTag.TenantId == CurrentTenant.Id &&
                    nodeTag.OwnerId == ownerId &&
                    nodeTag.TagId == input.TagId.Value)
                .Select(nodeTag => nodeTag.FileNodeId);

            assetJoinedQuery = assetJoinedQuery.Where(x => taggedNodeIds.Contains(x.Node.Id));
        }

        // Count for primary path
        var primaryTotalCount = await _asyncExecuter.LongCountAsync(assetJoinedQuery);

        // Ordered and paginated primary items
        var primaryItems = await _asyncExecuter.ToListAsync(
            assetJoinedQuery
                .OrderByDescending(x => x.Asset.TakenAt.HasValue ? x.Asset.TakenAt.Value : x.Node.CreationTime)
                .ThenBy(x => x.Node.NormalizedName));

        var primaryDtos = primaryItems
            .Select(x => FileCenterMediaLibraryHelpers.ToTimelineItem(x.Node, x.Asset))
            .ToList();

        // --- 兜底路径：无 MediaAsset 的媒体文件 ---
        // 只有没有 ProcessStatus/StartTime/EndTime 过滤时才需要兜底
        // 因为无 MediaAsset 的文件默认 Pending，且 TimelineTime = CreationTime
        var fallbackDtos = new List<MediaTimelineItemDto>();
        var fallbackTotalCount = 0L;

        var needsFallback = (!input.ProcessStatus.HasValue
            || input.ProcessStatus.Value == MediaAssetProcessStatus.Pending)
            && !excludeCompleted
            && !input.StartTime.HasValue
            && !input.EndTime.HasValue
            && (!input.MediaType.HasValue); // if media type specified, fallback can still match via extension

        if (needsFallback)
        {
            var mediaFileQuery = (await _fileNodeRepository.GetQueryableAsync())
                .Where(n =>
                    n.TenantId == CurrentTenant.Id &&
                    n.OwnerId == ownerId &&
                    n.NodeType == FileNodeType.File);

            // Apply media extension filter
            if (input.MediaType == MediaAssetMediaType.Image)
            {
                mediaFileQuery = ApplyImageFilter(mediaFileQuery);
            }
            else if (input.MediaType == MediaAssetMediaType.Video)
            {
                mediaFileQuery = ApplyVideoFilter(mediaFileQuery);
            }
            else
            {
                mediaFileQuery = ApplyMediaFilter(mediaFileQuery);
            }

            // Exclude nodes that already have any MediaAsset
            var allAssetsQuery = await _mediaAssetRepository.GetQueryableAsync();
            mediaFileQuery = mediaFileQuery.Where(n =>
                !allAssetsQuery.Any(a =>
                    a.FileNodeId == n.Id &&
                    a.OwnerId == ownerId &&
                    a.TenantId == CurrentTenant.Id));

            // IsFavorite filter
            if (input.IsFavorite.HasValue)
            {
                mediaFileQuery = mediaFileQuery.Where(n => n.IsFavorite == input.IsFavorite.Value);
            }

            // Album filter for fallback
            if (input.AlbumId.HasValue)
            {
                await GetOwnerAlbumAsync(ownerId, input.AlbumId.Value);
                var albumItemsQueryable = await _mediaAlbumItemRepository.GetQueryableAsync();
                var albumNodeIds = albumItemsQueryable
                    .Where(item =>
                        item.TenantId == CurrentTenant.Id &&
                        item.OwnerId == ownerId &&
                        item.AlbumId == input.AlbumId.Value)
                    .Select(item => item.FileNodeId);

                mediaFileQuery = mediaFileQuery.Where(n => albumNodeIds.Contains(n.Id));
            }

            // TagId filter for fallback
            if (input.TagId.HasValue)
            {
                var nodeTagsQueryable = await _nodeTagRepository.GetQueryableAsync();
                var taggedNodeIds = nodeTagsQueryable
                    .Where(nodeTag =>
                        nodeTag.TenantId == CurrentTenant.Id &&
                        nodeTag.OwnerId == ownerId &&
                        nodeTag.TagId == input.TagId.Value)
                    .Select(nodeTag => nodeTag.FileNodeId);

                mediaFileQuery = mediaFileQuery.Where(n => taggedNodeIds.Contains(n.Id));
            }

            fallbackTotalCount = await _asyncExecuter.LongCountAsync(mediaFileQuery);

            // Small window for fallback
            var fallbackItems = await _asyncExecuter.ToListAsync(
                mediaFileQuery
                    .OrderByDescending(n => n.CreationTime)
                    .ThenBy(n => n.NormalizedName)
                    .Take(FallbackMaxResultCount));

            fallbackDtos = fallbackItems
                .Select(n => FileCenterMediaLibraryHelpers.ToTimelineItem(n, null))
                .ToList();
        }

        // --- 合并两路结果 ---
        var allItems = primaryDtos.Concat(fallbackDtos)
            .OrderByDescending(item => item.TimelineTime)
            .ThenBy(item => item.Name)
            .ToList();

        var totalCount = primaryTotalCount + fallbackTotalCount;

        // Apply final pagination on combined result
        var pagedItems = allItems
            .Skip(input.SkipCount)
            .Take(maxResult)
            .ToList();

        return new PagedResultDto<MediaTimelineItemDto>(totalCount, pagedItems);
    }

    /// <summary>
    /// 合并的媒体过滤（图片+视频），可在数据库侧执行。
    /// </summary>
    private static IQueryable<FileNode> ApplyMediaFilter(IQueryable<FileNode> queryable)
    {
        return queryable.Where(node =>
            (node.ContentType != null && (node.ContentType.StartsWith("image/") || node.ContentType.StartsWith("video/"))) ||
            node.NormalizedName.EndsWith(".jpg") || node.NormalizedName.EndsWith(".jpeg") ||
            node.NormalizedName.EndsWith(".png") || node.NormalizedName.EndsWith(".gif") ||
            node.NormalizedName.EndsWith(".webp") || node.NormalizedName.EndsWith(".heic") ||
            node.NormalizedName.EndsWith(".heif") ||
            node.NormalizedName.EndsWith(".mp4") || node.NormalizedName.EndsWith(".m4v") ||
            node.NormalizedName.EndsWith(".mov") || node.NormalizedName.EndsWith(".mkv") ||
            node.NormalizedName.EndsWith(".webm") || node.NormalizedName.EndsWith(".avi"));
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
