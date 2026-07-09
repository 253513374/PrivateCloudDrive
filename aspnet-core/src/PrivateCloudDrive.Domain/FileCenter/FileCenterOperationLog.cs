using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心操作日志，记录对媒体资产（MediaAsset）的关键操作审计。
/// </summary>
public class FileCenterOperationLog : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid FileNodeId { get; private set; }

    public Guid MediaAssetId { get; private set; }

    /// <summary>
    /// 操作类型，如 MediaRetry。
    /// </summary>
    public string Action { get; private set; } = null!;

    /// <summary>
    /// 操作前的 MediaAsset.ProcessStatus 值。
    /// </summary>
    public string StatusBefore { get; private set; } = null!;

    /// <summary>
    /// 操作后的 MediaAsset.ProcessStatus 值。
    /// </summary>
    public string StatusAfter { get; private set; } = null!;

    public Guid? OperatorUserId { get; private set; }

    protected FileCenterOperationLog()
    {
    }

    public FileCenterOperationLog(
        Guid id,
        Guid? tenantId,
        Guid fileNodeId,
        Guid mediaAssetId,
        string action,
        string statusBefore,
        string statusAfter,
        Guid? operatorUserId)
        : base(id)
    {
        TenantId = tenantId;
        FileNodeId = fileNodeId;
        MediaAssetId = mediaAssetId;
        Action = Check.NotNullOrWhiteSpace(action, nameof(action));
        StatusBefore = Check.NotNullOrWhiteSpace(statusBefore, nameof(statusBefore));
        StatusAfter = Check.NotNullOrWhiteSpace(statusAfter, nameof(statusAfter));
        OperatorUserId = operatorUserId;
    }
}
