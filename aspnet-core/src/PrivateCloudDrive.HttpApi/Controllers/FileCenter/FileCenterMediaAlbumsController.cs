using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 媒体相册 HTTP API 控制器。
/// </summary>
[Route("api/file-center/media/albums")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterMediaAlbumsController : PrivateCloudDriveController
{
    private readonly IFileCenterMediaAlbumsAppService _albumsAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterMediaAlbumsController"/> 的新实例。
    /// </summary>
    public FileCenterMediaAlbumsController(IFileCenterMediaAlbumsAppService albumsAppService)
    {
        _albumsAppService = albumsAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<MediaAlbumDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _albumsAppService.GetListAsync(input);
    }

    [HttpGet("{id:guid}")]
    public virtual Task<MediaAlbumDto> GetAsync(Guid id)
    {
        return _albumsAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<MediaAlbumDto> CreateAsync([FromBody] CreateMediaAlbumInput input)
    {
        return _albumsAppService.CreateAsync(input);
    }

    [HttpPut("{id:guid}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<MediaAlbumDto> UpdateAsync(Guid id, [FromBody] UpdateMediaAlbumInput input)
    {
        return _albumsAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _albumsAppService.DeleteAsync(id);
    }

    [HttpGet("{id:guid}/items")]
    public virtual Task<PagedResultDto<MediaTimelineItemDto>> GetItemsAsync(
        Guid id,
        [FromQuery] PagedResultRequestDto input)
    {
        return _albumsAppService.GetItemsAsync(id, input);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<IReadOnlyList<MediaTimelineItemDto>> AddItemsAsync(
        Guid id,
        [FromBody] AddMediaAlbumItemsInput input)
    {
        return _albumsAppService.AddItemsAsync(id, input);
    }

    [HttpDelete("{id:guid}/items/{fileNodeId:guid}")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task RemoveItemAsync(Guid id, Guid fileNodeId)
    {
        return _albumsAppService.RemoveItemAsync(id, fileNodeId);
    }

    [HttpPost("{id:guid}/cover")]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual Task<MediaAlbumDto> SetCoverAsync(Guid id, [FromBody] SetMediaAlbumCoverInput input)
    {
        return _albumsAppService.SetCoverAsync(id, input);
    }
}
