using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 提供IMobileAuthAuditLogs相关应用服务编排，承接权限校验、业务规则调用与 DTO 映射。
/// </summary>
public interface IMobileAuthAuditLogsAppService : IApplicationService
{
    Task RecordAsync(CreateMobileAuthAuditLogInput input);

    Task<PagedResultDto<MobileAuthAuditLogDto>> GetListAsync(PagedResultRequestDto input);
}
