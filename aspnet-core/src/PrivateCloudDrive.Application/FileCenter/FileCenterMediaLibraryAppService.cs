using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
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
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="FileCenterMediaLibraryAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterMediaLibraryAppService(
        IRepository<FileNode, Guid> fileNodeRepository,
        IRepository<FileNodeTag, Guid> nodeTagRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _fileNodeRepository = fileNodeRepository;
        _nodeTagRepository = nodeTagRepository;
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
