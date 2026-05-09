using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 媒体库 HTTP API 控制器。
/// </summary>
[Route("api/file-center/media")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterMediaLibraryController : PrivateCloudDriveController
{
    private readonly IFileCenterMediaLibraryAppService _mediaLibraryAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterMediaLibraryController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterMediaLibraryController(IFileCenterMediaLibraryAppService mediaLibraryAppService)
    {
        _mediaLibraryAppService = mediaLibraryAppService;
    }

    /// <summary>
    /// 查询图片媒体列表。
    /// </summary>
    [HttpGet("images")]
    public virtual Task<PagedResultDto<FileNodeDto>> GetImagesAsync([FromQuery] GetMediaFilesInput input)
    {
        return _mediaLibraryAppService.GetImagesAsync(input);
    }

    /// <summary>
    /// 查询视频媒体列表。
    /// </summary>
    [HttpGet("videos")]
    public virtual Task<PagedResultDto<FileNodeDto>> GetVideosAsync([FromQuery] GetMediaFilesInput input)
    {
        return _mediaLibraryAppService.GetVideosAsync(input);
    }

    /// <summary>
    /// 查询媒体时间线。
    /// </summary>
    [HttpGet("timeline")]
    public virtual Task<PagedResultDto<MediaTimelineItemDto>> GetTimelineAsync([FromQuery] GetMediaTimelineInput input)
    {
        return _mediaLibraryAppService.GetTimelineAsync(input);
    }

    /// <summary>
    /// 查询媒体详情。
    /// </summary>
    [HttpGet("{fileNodeId:guid}/detail")]
    public virtual Task<MediaDetailDto> GetDetailAsync(Guid fileNodeId)
    {
        return _mediaLibraryAppService.GetDetailAsync(fileNodeId);
    }

    /// <summary>
    /// 查询媒体处理状态。
    /// </summary>
    [HttpGet("processing-status")]
    public virtual Task<PagedResultDto<MediaTimelineItemDto>> GetProcessingStatusAsync(
        [FromQuery] GetMediaProcessingStatusInput input)
    {
        return _mediaLibraryAppService.GetProcessingStatusAsync(input);
    }

    /// <summary>
    /// 重新投递媒体处理任务。
    /// </summary>
    [HttpPost("{fileNodeId:guid}/retry-processing")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<MediaDetailDto> RetryProcessingAsync(Guid fileNodeId)
    {
        return _mediaLibraryAppService.RetryProcessingAsync(fileNodeId);
    }
}
