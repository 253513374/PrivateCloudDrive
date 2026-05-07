using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterTagsAppService : IApplicationService
{
    Task<IReadOnlyList<FileTagDto>> GetListAsync();

    Task<FileTagDto> CreateAsync(CreateFileTagInput input);

    Task<FileTagDto> UpdateAsync(Guid id, UpdateFileTagInput input);

    Task DeleteAsync(Guid id);

    Task AddToNodeAsync(Guid nodeId, Guid tagId);

    Task RemoveFromNodeAsync(Guid nodeId, Guid tagId);

    Task<FileNodeDto> SetFavoriteAsync(Guid nodeId, SetFileFavoriteInput input);
}
