using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.MobileAuth;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.MobileAuth;

[Route("api/mobile-auth/audit-logs")]
public class MobileAuthAuditLogsController : PrivateCloudDriveController
{
    private readonly IMobileAuthAuditLogsAppService _auditLogsAppService;

    public MobileAuthAuditLogsController(IMobileAuthAuditLogsAppService auditLogsAppService)
    {
        _auditLogsAppService = auditLogsAppService;
    }

    [HttpPost]
    [AllowAnonymous]
    public virtual Task RecordAsync([FromBody] CreateMobileAuthAuditLogInput input)
    {
        return _auditLogsAppService.RecordAsync(input);
    }

    [HttpGet]
    [Authorize(PrivateCloudDrivePermissions.MobileAuth.AuditLogs)]
    public virtual Task<PagedResultDto<MobileAuthAuditLogDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _auditLogsAppService.GetListAsync(input);
    }
}
