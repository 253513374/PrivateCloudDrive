using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.MobileAuth;
using PrivateCloudDrive.Permissions;
using Volo.Abp.AuditLogging;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.OperationLogs;

[Authorize(PrivateCloudDrivePermissions.OperationLogs.View)]
public class OperationLogsAppService : PrivateCloudDriveAppService, IOperationLogsAppService
{
    private readonly IRepository<AuditLog, Guid> _auditLogRepository;
    private readonly IRepository<IdentitySecurityLog, Guid> _securityLogRepository;
    private readonly IRepository<MobileAuthAuditLog, Guid> _mobileAuthAuditLogRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public OperationLogsAppService(
        IRepository<AuditLog, Guid> auditLogRepository,
        IRepository<IdentitySecurityLog, Guid> securityLogRepository,
        IRepository<MobileAuthAuditLog, Guid> mobileAuthAuditLogRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _auditLogRepository = auditLogRepository;
        _securityLogRepository = securityLogRepository;
        _mobileAuthAuditLogRepository = mobileAuthAuditLogRepository;
        _asyncExecuter = asyncExecuter;
    }

    public virtual async Task<PagedResultDto<OperationLogDto>> GetListAsync(GetOperationLogsInput input)
    {
        var logs = new List<OperationLogDto>();

        logs.AddRange(await GetAuditActionLogsAsync(input));
        logs.AddRange(await GetSecurityLogsAsync(input));
        logs.AddRange(await GetMobileAuthLogsAsync(input));

        var filteredLogs = ApplyInMemoryFilters(logs, input);
        var totalCount = filteredLogs.LongCount();
        var items = ApplySorting(filteredLogs, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<OperationLogDto>(totalCount, items);
    }

    private async Task<IReadOnlyList<OperationLogDto>> GetAuditActionLogsAsync(GetOperationLogsInput input)
    {
        if (!MatchesFilter(input.Source, OperationLogSources.AuditLog))
        {
            return Array.Empty<OperationLogDto>();
        }

        var query = (await _auditLogRepository.WithDetailsAsync(auditLog => auditLog.Actions))
            .Where(item => item.TenantId == CurrentTenant.Id);

        query = ApplyAuditLogDatabaseFilters(query, input);

        var auditLogs = await _asyncExecuter.ToListAsync(query);
        var records = auditLogs.SelectMany(auditLog => auditLog.Actions.Select(action => new AuditActionReadModel
            {
                Id = action.Id,
                TenantId = auditLog.TenantId,
                Time = action.ExecutionTime,
                UserId = auditLog.UserId,
                UserName = auditLog.UserName,
                ClientId = auditLog.ClientId,
                ClientIpAddress = auditLog.ClientIpAddress,
                HttpStatusCode = auditLog.HttpStatusCode,
                CorrelationId = auditLog.CorrelationId,
                ServiceName = action.ServiceName,
                MethodName = action.MethodName,
                Exceptions = auditLog.Exceptions
            }))
            .ToList();

        return records
            .Where(IsReportableAuditAction)
            .Select(ToOperationLog)
            .ToList();
    }

    private async Task<IReadOnlyList<OperationLogDto>> GetSecurityLogsAsync(GetOperationLogsInput input)
    {
        if (!MatchesFilter(input.Source, OperationLogSources.SecurityLog))
        {
            return Array.Empty<OperationLogDto>();
        }

        var query = (await _securityLogRepository.GetQueryableAsync())
            .Where(item => item.TenantId == CurrentTenant.Id);

        query = ApplySecurityLogDatabaseFilters(query, input);

        var records = await _asyncExecuter.ToListAsync(query);

        return records.Select(log => new OperationLogDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                Time = log.CreationTime,
                Source = OperationLogSources.SecurityLog,
                Action = string.IsNullOrWhiteSpace(log.Action)
                    ? OperationLogActions.Security
                    : log.Action,
                Result = GetSecurityLogResult(log.Action),
                UserId = log.UserId,
                UserName = log.UserName,
                ClientId = log.ClientId,
                ClientIpAddress = log.ClientIpAddress,
                CorrelationId = log.CorrelationId,
                Summary = CreateSecuritySummary(log)
            })
            .ToList();
    }

    private async Task<IReadOnlyList<OperationLogDto>> GetMobileAuthLogsAsync(GetOperationLogsInput input)
    {
        if (!MatchesFilter(input.Source, OperationLogSources.MobileAuth))
        {
            return Array.Empty<OperationLogDto>();
        }

        var query = (await _mobileAuthAuditLogRepository.GetQueryableAsync())
            .Where(item => item.TenantId == CurrentTenant.Id);

        query = ApplyMobileAuthDatabaseFilters(query, input);

        var records = await _asyncExecuter.ToListAsync(query);

        return records.Select(log => new OperationLogDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                Time = log.CreationTime,
                Source = OperationLogSources.MobileAuth,
                Action = log.Action,
                Result = log.Result,
                UserId = log.UserId,
                UserName = log.UserName,
                ClientId = log.ClientId,
                Summary = $"{log.Provider}:{log.Action}:{log.Result}"
            })
            .ToList();
    }

    private static IQueryable<AuditLog> ApplyAuditLogDatabaseFilters(
        IQueryable<AuditLog> query,
        GetOperationLogsInput input)
    {
        if (input.UserId.HasValue)
        {
            query = query.Where(item => item.UserId == input.UserId);
        }

        if (input.StartTime.HasValue)
        {
            query = query.Where(item => item.ExecutionTime >= input.StartTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            query = query.Where(item => item.ExecutionTime <= input.EndTime.Value);
        }

        return query;
    }

    private static IQueryable<IdentitySecurityLog> ApplySecurityLogDatabaseFilters(
        IQueryable<IdentitySecurityLog> query,
        GetOperationLogsInput input)
    {
        if (input.UserId.HasValue)
        {
            query = query.Where(item => item.UserId == input.UserId);
        }

        if (input.StartTime.HasValue)
        {
            query = query.Where(item => item.CreationTime >= input.StartTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            query = query.Where(item => item.CreationTime <= input.EndTime.Value);
        }

        return query;
    }

    private static IQueryable<MobileAuthAuditLog> ApplyMobileAuthDatabaseFilters(
        IQueryable<MobileAuthAuditLog> query,
        GetOperationLogsInput input)
    {
        if (input.UserId.HasValue)
        {
            query = query.Where(item => item.UserId == input.UserId);
        }

        if (input.StartTime.HasValue)
        {
            query = query.Where(item => item.CreationTime >= input.StartTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            query = query.Where(item => item.CreationTime <= input.EndTime.Value);
        }

        return query;
    }

    private static IEnumerable<OperationLogDto> ApplyInMemoryFilters(
        IEnumerable<OperationLogDto> logs,
        GetOperationLogsInput input)
    {
        var query = logs;

        if (!string.IsNullOrWhiteSpace(input.Source))
        {
            query = query.Where(item => MatchesFilter(input.Source, item.Source));
        }

        if (!string.IsNullOrWhiteSpace(input.Action))
        {
            query = query.Where(item => MatchesFilter(input.Action, item.Action));
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            query = query.Where(item =>
                item.UserName != null &&
                item.UserName.Contains(input.UserName, StringComparison.OrdinalIgnoreCase));
        }

        if (input.StartTime.HasValue)
        {
            query = query.Where(item => item.Time >= input.StartTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            query = query.Where(item => item.Time <= input.EndTime.Value);
        }

        return query.ToList();
    }

    private static IEnumerable<OperationLogDto> ApplySorting(
        IEnumerable<OperationLogDto> logs,
        string? sorting)
    {
        return NormalizeSorting(sorting) switch
        {
            "time asc" => logs.OrderBy(item => item.Time),
            "source asc" => logs.OrderBy(item => item.Source).ThenByDescending(item => item.Time),
            "source desc" => logs.OrderByDescending(item => item.Source).ThenByDescending(item => item.Time),
            "action asc" => logs.OrderBy(item => item.Action).ThenByDescending(item => item.Time),
            "action desc" => logs.OrderByDescending(item => item.Action).ThenByDescending(item => item.Time),
            "username asc" => logs.OrderBy(item => item.UserName).ThenByDescending(item => item.Time),
            "username desc" => logs.OrderByDescending(item => item.UserName).ThenByDescending(item => item.Time),
            _ => logs.OrderByDescending(item => item.Time)
        };
    }

    private static OperationLogDto ToOperationLog(AuditActionReadModel action)
    {
        var serviceName = action.ServiceName ?? string.Empty;
        var methodName = action.MethodName ?? string.Empty;

        return new OperationLogDto
        {
            Id = action.Id,
            TenantId = action.TenantId,
            Time = action.Time,
            Source = OperationLogSources.AuditLog,
            Action = ClassifyAuditAction(serviceName, methodName),
            Result = GetAuditActionResult(action.HttpStatusCode, action.Exceptions),
            UserId = action.UserId,
            UserName = action.UserName,
            ClientId = action.ClientId,
            ClientIpAddress = action.ClientIpAddress,
            HttpStatusCode = action.HttpStatusCode,
            CorrelationId = action.CorrelationId,
            Summary = CreateAuditActionSummary(serviceName, methodName)
        };
    }

    private static bool IsReportableAuditAction(AuditActionReadModel action)
    {
        var serviceName = action.ServiceName ?? string.Empty;

        if (serviceName.Contains(nameof(OperationLogsAppService), StringComparison.OrdinalIgnoreCase) ||
            serviceName.Contains(nameof(MobileAuthAuditLogsAppService), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return serviceName.Contains("FileCenter", StringComparison.OrdinalIgnoreCase) ||
               serviceName.Contains("Identity", StringComparison.OrdinalIgnoreCase) ||
               serviceName.Contains("PermissionManagement", StringComparison.OrdinalIgnoreCase) ||
               serviceName.Contains("SettingManagement", StringComparison.OrdinalIgnoreCase) ||
               serviceName.Contains("TenantManagement", StringComparison.OrdinalIgnoreCase);
    }

    private static string ClassifyAuditAction(string serviceName, string methodName)
    {
        if (serviceName.Contains("FileCenterFileUpload", StringComparison.OrdinalIgnoreCase) ||
            serviceName.Contains("FileCenterChunkUpload", StringComparison.OrdinalIgnoreCase))
        {
            return OperationLogActions.FileUpload;
        }

        if (serviceName.Contains("FileCenterFileDownload", StringComparison.OrdinalIgnoreCase))
        {
            return OperationLogActions.FileDownload;
        }

        if (serviceName.Contains("FileCenterFolders", StringComparison.OrdinalIgnoreCase))
        {
            return methodName switch
            {
                "CreateAsync" => OperationLogActions.FolderCreate,
                "DeleteAsync" => OperationLogActions.FileDelete,
                "RestoreAsync" => OperationLogActions.FileRestore,
                "PermanentlyDeleteAsync" => OperationLogActions.FilePermanentDelete,
                "EmptyTrashAsync" => OperationLogActions.TrashEmpty,
                _ => CreateFallbackAction(serviceName, methodName)
            };
        }

        if (serviceName.Contains("FileCenterShares", StringComparison.OrdinalIgnoreCase))
        {
            return methodName switch
            {
                "CreateAsync" => OperationLogActions.ShareCreate,
                "DeleteAsync" => OperationLogActions.ShareDelete,
                _ => CreateFallbackAction(serviceName, methodName)
            };
        }

        if (serviceName.Contains("FileCenterPublicShares", StringComparison.OrdinalIgnoreCase))
        {
            return methodName.Contains("Download", StringComparison.OrdinalIgnoreCase)
                ? OperationLogActions.ShareDownload
                : OperationLogActions.ShareAccess;
        }

        if (serviceName.Contains("FileCenterTags", StringComparison.OrdinalIgnoreCase))
        {
            return methodName switch
            {
                "CreateAsync" => OperationLogActions.TagCreate,
                "UpdateAsync" => OperationLogActions.TagUpdate,
                "DeleteAsync" => OperationLogActions.TagDelete,
                "AddToNodeAsync" => OperationLogActions.TagAddToFile,
                "RemoveFromNodeAsync" => OperationLogActions.TagRemoveFromFile,
                "SetFavoriteAsync" => OperationLogActions.FavoriteSet,
                _ => CreateFallbackAction(serviceName, methodName)
            };
        }

        if (serviceName.Contains("Identity", StringComparison.OrdinalIgnoreCase) ||
            serviceName.Contains("PermissionManagement", StringComparison.OrdinalIgnoreCase) ||
            serviceName.Contains("SettingManagement", StringComparison.OrdinalIgnoreCase) ||
            serviceName.Contains("TenantManagement", StringComparison.OrdinalIgnoreCase))
        {
            return OperationLogActions.Security;
        }

        return CreateFallbackAction(serviceName, methodName);
    }

    private static string CreateFallbackAction(string serviceName, string methodName)
    {
        var service = serviceName.Split('.').LastOrDefault() ?? "UnknownService";
        var method = string.IsNullOrWhiteSpace(methodName) ? "UnknownMethod" : methodName;

        return $"{service}.{method}";
    }

    private static string CreateAuditActionSummary(string serviceName, string methodName)
    {
        return $"{TrimServiceName(serviceName)}.{methodName}";
    }

    private static string CreateSecuritySummary(IdentitySecurityLog log)
    {
        var identity = string.IsNullOrWhiteSpace(log.Identity) ? "Identity" : log.Identity;
        var action = string.IsNullOrWhiteSpace(log.Action) ? OperationLogActions.Security : log.Action;

        return $"{identity}:{action}";
    }

    private static string TrimServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return "UnknownService";
        }

        return serviceName.Split('.').LastOrDefault() ?? serviceName;
    }

    private static string GetAuditActionResult(int? httpStatusCode, string? exceptions)
    {
        if (!string.IsNullOrWhiteSpace(exceptions) ||
            (httpStatusCode.HasValue && httpStatusCode.Value >= 400))
        {
            return MobileAuthAuditLogResults.Failed;
        }

        return MobileAuthAuditLogResults.Success;
    }

    private static string GetSecurityLogResult(string? action)
    {
        if (!string.IsNullOrWhiteSpace(action) &&
            action.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return MobileAuthAuditLogResults.Failed;
        }

        return MobileAuthAuditLogResults.Success;
    }

    private static string NormalizeSorting(string? sorting)
    {
        return string.IsNullOrWhiteSpace(sorting)
            ? string.Empty
            : sorting.Trim().Replace("creationTime", "time", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    }

    private static bool MatchesFilter(string? expected, string actual)
    {
        return string.IsNullOrWhiteSpace(expected) ||
               string.Equals(expected.Trim(), actual, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class AuditActionReadModel
    {
        public Guid Id { get; set; }

        public Guid? TenantId { get; set; }

        public DateTime Time { get; set; }

        public Guid? UserId { get; set; }

        public string? UserName { get; set; }

        public string? ClientId { get; set; }

        public string? ClientIpAddress { get; set; }

        public int? HttpStatusCode { get; set; }

        public string? CorrelationId { get; set; }

        public string? ServiceName { get; set; }

        public string? MethodName { get; set; }

        public string? Exceptions { get; set; }
    }
}
