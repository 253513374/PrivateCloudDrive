using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.MobileAuth;

public class MobileAuthAuditLog : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public string? UserName { get; private set; }

    public string Provider { get; private set; } = null!;

    public string Action { get; private set; } = null!;

    public string Result { get; private set; } = null!;

    public string? FailureReason { get; private set; }

    public string? ClientId { get; private set; }

    public string? DeviceIdHash { get; private set; }

    public string? UserAgent { get; private set; }

    protected MobileAuthAuditLog()
    {
    }

    public MobileAuthAuditLog(
        Guid id,
        Guid? tenantId,
        Guid? userId,
        string? userName,
        string provider,
        string action,
        string result,
        string? failureReason = null,
        string? clientId = null,
        string? deviceIdHash = null,
        string? userAgent = null)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = Check.Length(userName, nameof(userName), MobileAuthAuditLogConsts.MaxUserNameLength);
        Provider = Check.Length(
            Check.NotNullOrWhiteSpace(provider, nameof(provider)),
            nameof(provider),
            MobileAuthAuditLogConsts.MaxProviderLength)!;
        Action = Check.Length(
            Check.NotNullOrWhiteSpace(action, nameof(action)),
            nameof(action),
            MobileAuthAuditLogConsts.MaxActionLength)!;
        Result = Check.Length(
            Check.NotNullOrWhiteSpace(result, nameof(result)),
            nameof(result),
            MobileAuthAuditLogConsts.MaxResultLength)!;
        FailureReason = Check.Length(failureReason, nameof(failureReason), MobileAuthAuditLogConsts.MaxFailureReasonLength);
        ClientId = Check.Length(clientId, nameof(clientId), MobileAuthAuditLogConsts.MaxClientIdLength);
        DeviceIdHash = Check.Length(deviceIdHash, nameof(deviceIdHash), MobileAuthAuditLogConsts.MaxDeviceIdHashLength);
        UserAgent = Check.Length(userAgent, nameof(userAgent), MobileAuthAuditLogConsts.MaxUserAgentLength);
    }
}
