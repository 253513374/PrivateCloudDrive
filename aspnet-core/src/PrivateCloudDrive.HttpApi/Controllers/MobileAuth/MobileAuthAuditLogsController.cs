using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.MobileAuth;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.MobileAuth;

/// <summary>
/// 提供MobileAuthAuditLogs相关 HTTP API 入口，负责请求绑定并委托应用服务处理业务逻辑。
/// </summary>
[Route("api/mobile-auth/audit-logs")]
public class MobileAuthAuditLogsController : PrivateCloudDriveController
{
    private readonly IMobileAuthAuditLogsAppService _auditLogsAppService;

    /// <summary>
    /// 初始化 <see cref="MobileAuthAuditLogsController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MobileAuthAuditLogsController(IMobileAuthAuditLogsAppService auditLogsAppService)
    {
        _auditLogsAppService = auditLogsAppService;
    }

    /// <summary>
    /// 记录业务事件或安全事件，便于后续审计、追踪和风险分析。
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public virtual Task RecordAsync([FromBody] CreateMobileAuthAuditLogInput input)
    {
        return _auditLogsAppService.RecordAsync(input);
    }

    /// <summary>
    /// 查询分页列表数据，并按当前用户、租户和输入条件进行过滤。
    /// </summary>
    [HttpGet]
    [Authorize(PrivateCloudDrivePermissions.MobileAuth.AuditLogs)]
    public virtual Task<PagedResultDto<MobileAuthAuditLogDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _auditLogsAppService.GetListAsync(input);
    }
}
