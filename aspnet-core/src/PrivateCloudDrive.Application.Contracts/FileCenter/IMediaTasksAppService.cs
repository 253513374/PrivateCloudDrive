using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体处理任务管理应用服务契约。
/// </summary>
public interface IMediaTasksAppService : IApplicationService
{
    /// <summary>
    /// 获取媒体处理任务分页列表。
    /// </summary>
    Task<PagedResultDto<MediaTaskDto>> GetListAsync(GetMediaTasksInput input);

    /// <summary>
    /// 重新处理失败的任务。
    /// </summary>
    Task RetryAsync(Guid mediaAssetId);
}
