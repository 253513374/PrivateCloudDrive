namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 回收站存储空间摘要，包含已用字节数和超过保留天数的项目计数。
/// </summary>
public sealed record TrashStorageSummary(
    long UsedBytes,
    int ItemsOverThresholdCount,
    int RetentionDays,
    string CleanupSuggestion);
