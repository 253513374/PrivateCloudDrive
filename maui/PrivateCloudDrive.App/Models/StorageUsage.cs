namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 当前账号的文件中心容量使用摘要。
/// </summary>
public sealed record StorageUsage(
    long UsedBytes,
    long QuotaBytes,
    long RemainingBytes,
    decimal UsagePercent,
    bool IsQuotaConfigured);
