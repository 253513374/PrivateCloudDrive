namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 文件中心系统健康状态，用于设置页展示后端与存储可用性。
/// </summary>
public enum SystemHealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2
}

/// <summary>
/// 文件中心系统健康摘要。
/// </summary>
public sealed record SystemHealthSummary(
    SystemHealthStatus OverallStatus,
    SystemHealthStatus ApiStatus,
    SystemHealthStatus StorageStatus,
    SystemHealthStatus FfmpegStatus,
    SystemHealthStatus FfprobeStatus,
    string StorageProvider,
    long StorageUsedBytes,
    long StorageQuotaBytes,
    bool IsQuotaConfigured,
    DateTime GeneratedAt,
    IReadOnlyList<string> Diagnostics);
