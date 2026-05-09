using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 提供MobileAuthAuditLogs相关应用服务编排，承接权限校验、业务规则调用与 DTO 映射。
/// </summary>
public class MobileAuthAuditLogsAppService : PrivateCloudDriveAppService, IMobileAuthAuditLogsAppService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<MobileAuthAuditLog, Guid> _auditLogRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="MobileAuthAuditLogsAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MobileAuthAuditLogsAppService(
        IGuidGenerator guidGenerator,
        IRepository<MobileAuthAuditLog, Guid> auditLogRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _guidGenerator = guidGenerator;
        _auditLogRepository = auditLogRepository;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 记录业务事件或安全事件，便于后续审计、追踪和风险分析。
    /// </summary>
    [AllowAnonymous]
    public virtual async Task RecordAsync(CreateMobileAuthAuditLogInput input)
    {
        var auditLog = new MobileAuthAuditLog(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            CurrentUser.Id,
            NormalizeUserName(input.UserName),
            input.Provider,
            input.Action,
            input.Result,
            input.FailureReason,
            input.ClientId,
            input.DeviceIdHash,
            input.UserAgent);

        await _auditLogRepository.InsertAsync(auditLog, autoSave: true);
    }

    /// <summary>
    /// 查询分页列表数据，并按当前用户、租户和输入条件进行过滤。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.MobileAuth.AuditLogs)]
    public virtual async Task<PagedResultDto<MobileAuthAuditLogDto>> GetListAsync(PagedResultRequestDto input)
    {
        var queryable = (await _auditLogRepository.GetQueryableAsync())
            .Where(item => item.TenantId == CurrentTenant.Id)
            .OrderByDescending(item => item.CreationTime);

        var totalCount = await _asyncExecuter.LongCountAsync(queryable);
        var items = await _asyncExecuter.ToListAsync(
            queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<MobileAuthAuditLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    private static MobileAuthAuditLogDto ToDto(MobileAuthAuditLog auditLog)
    {
        return new MobileAuthAuditLogDto
        {
            Id = auditLog.Id,
            TenantId = auditLog.TenantId,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            Provider = auditLog.Provider,
            Action = auditLog.Action,
            Result = auditLog.Result,
            FailureReason = auditLog.FailureReason,
            ClientId = auditLog.ClientId,
            DeviceIdHash = auditLog.DeviceIdHash,
            UserAgent = auditLog.UserAgent,
            CreationTime = auditLog.CreationTime
        };
    }

    private static string? NormalizeUserName(string? userName)
    {
        return string.IsNullOrWhiteSpace(userName)
            ? null
            : userName.Trim();
    }
}
