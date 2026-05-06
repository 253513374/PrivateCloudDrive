using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterFoldersAppService : IApplicationService
{
    Task<FileNodeDto> CreateAsync(CreateFolderInput input);

    Task<PagedResultDto<FileNodeDto>> GetListAsync(GetFolderChildrenInput input);

    Task<FileNodeDto> RenameAsync(Guid id, RenameFileNodeInput input);

    Task<FileNodeDto> MoveAsync(Guid id, MoveFileNodeInput input);

    Task DeleteAsync(Guid id);
}
