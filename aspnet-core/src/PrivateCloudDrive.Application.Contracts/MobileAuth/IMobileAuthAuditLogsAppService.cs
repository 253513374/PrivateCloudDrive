using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.MobileAuth;

public interface IMobileAuthAuditLogsAppService : IApplicationService
{
    Task RecordAsync(CreateMobileAuthAuditLogInput input);

    Task<PagedResultDto<MobileAuthAuditLogDto>> GetListAsync(PagedResultRequestDto input);
}
