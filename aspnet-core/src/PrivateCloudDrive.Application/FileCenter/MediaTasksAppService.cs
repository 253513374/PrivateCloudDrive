using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体处理任务管理应用服务，管理员可查看任务队列状态、失败原因和重新处理。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class MediaTasksAppService : PrivateCloudDriveAppService, IMediaTasksAppService
{
    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public MediaTasksAppService(
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 获取媒体处理任务分页列表，支持按状态筛选。
    /// </summary>
    public virtual async Task<PagedResultDto<MediaTaskDto>> GetListAsync(GetMediaTasksInput input)
    {
        var query = await _mediaAssetRepository.GetQueryableAsync();

        // 按租户过滤
        query = query.Where(item => item.TenantId == CurrentTenant.Id);

        // 按状态筛选
        if (!string.IsNullOrWhiteSpace(input.ProcessStatus))
        {
            if (Enum.TryParse<MediaAssetProcessStatus>(input.ProcessStatus, ignoreCase: true, out var status))
            {
                query = query.Where(item => item.ProcessStatus == status);
            }
        }

        var totalCount = await _asyncExecuter.LongCountAsync(query);

        var items = await _asyncExecuter.ToListAsync(
            query
                .OrderByDescending(item => item.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        var dtos = items.Select(item => new MediaTaskDto
        {
            Id = item.Id,
            FileNodeId = item.FileNodeId,
            MediaType = item.MediaType.ToString(),
            ProcessStatus = item.ProcessStatus.ToString(),
            ProcessError = item.ProcessError,
            CreationTime = item.CreationTime,
            LastModificationTime = item.LastModificationTime
        }).ToList();

        return new PagedResultDto<MediaTaskDto>(totalCount, dtos);
    }

    /// <summary>
    /// 重新处理失败的任务。将 Failed 状态的任务重置为 Processing，
    /// 后台处理任务负责实际重新处理。
    /// </summary>
    public virtual async Task RetryAsync(Guid mediaAssetId)
    {
        var asset = await _mediaAssetRepository.GetAsync(mediaAssetId);

        if (asset.ProcessStatus != MediaAssetProcessStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot retry media asset in status {asset.ProcessStatus}. Only Failed tasks can be retried.");
        }

        // MarkProcessing 允许从 Failed → Processing，清空 ProcessError
        asset.MarkProcessing();
        await _mediaAssetRepository.UpdateAsync(asset);
    }
}
