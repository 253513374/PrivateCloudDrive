using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件夹、目录树和回收站应用服务契约。
/// </summary>
public interface IFileCenterFoldersAppService : IApplicationService
{
    Task<FileNodeDto> CreateAsync(CreateFolderInput input);

    Task<PagedResultDto<FileNodeDto>> GetListAsync(GetFolderChildrenInput input);

    Task<PagedResultDto<FileNodeDto>> GetDeletedListAsync(PagedResultRequestDto input);

    Task<FileNodeDto> RenameAsync(Guid id, RenameFileNodeInput input);

    Task<FileNodeDto> MoveAsync(Guid id, MoveFileNodeInput input);

    Task<IReadOnlyList<FileNodeDto>> MoveManyAsync(BatchMoveFileNodesInput input);

    Task DeleteAsync(Guid id);

    Task DeleteManyAsync(BatchFileNodeInput input);

    Task<FileNodeDto> RestoreAsync(Guid id);

    Task<IReadOnlyList<FileNodeDto>> RestoreManyAsync(BatchFileNodeInput input);

    Task PermanentDeleteAsync(Guid id);

    Task PermanentDeleteManyAsync(BatchFileNodeInput input);

    Task<IReadOnlyList<FileNodeDto>> SetFavoriteManyAsync(BatchSetFavoriteInput input);

    Task EmptyTrashAsync();
}
