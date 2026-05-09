using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 登录用户分享管理应用服务契约。
/// </summary>
public interface IFileCenterSharesAppService : IApplicationService
{
    Task<FileShareDto> CreateAsync(CreateFileShareInput input);

    Task<PagedResultDto<FileShareDto>> GetListAsync(PagedResultRequestDto input);

    Task<PagedResultDto<FileShareDto>> GetAllListAsync(PagedResultRequestDto input);

    Task DeleteAsync(Guid id);

    Task DisableAsync(Guid id);
}
