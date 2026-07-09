namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 分享风险摘要，由后端聚合不可过期分享、公开分享和长期未使用分享的风险指标。
/// </summary>
public sealed record ShareRiskSummary(
    int NoExpiryShareCount,
    int PublicShareCount,
    int LongUnusedShareCount,
    string NoExpiryWarning,
    string PublicWarning,
    string LongUnusedWarning);
