using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterSharesAppService : IApplicationService
{
    Task<FileShareDto> CreateAsync(CreateFileShareInput input);

    Task<PagedResultDto<FileShareDto>> GetListAsync(PagedResultRequestDto input);

    Task<PagedResultDto<FileShareDto>> GetAllListAsync(PagedResultRequestDto input);

    Task DeleteAsync(Guid id);

    Task DisableAsync(Guid id);
}
