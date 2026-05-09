using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体相册应用服务契约。
/// </summary>
public interface IFileCenterMediaAlbumsAppService : IApplicationService
{
    Task<PagedResultDto<MediaAlbumDto>> GetListAsync(PagedResultRequestDto input);

    Task<MediaAlbumDto> GetAsync(Guid id);

    Task<MediaAlbumDto> CreateAsync(CreateMediaAlbumInput input);

    Task<MediaAlbumDto> UpdateAsync(Guid id, UpdateMediaAlbumInput input);

    Task DeleteAsync(Guid id);

    Task<PagedResultDto<MediaTimelineItemDto>> GetItemsAsync(Guid id, PagedResultRequestDto input);

    Task<IReadOnlyList<MediaTimelineItemDto>> AddItemsAsync(Guid id, AddMediaAlbumItemsInput input);

    Task RemoveItemAsync(Guid id, Guid fileNodeId);

    Task<MediaAlbumDto> SetCoverAsync(Guid id, SetMediaAlbumCoverInput input);
}
