using System;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 分享风险提示 DTO，包含风险指标计数和可读文案。
/// 管理员可查看任意用户的风险，普通用户只能查看自己的风险。
/// 不返回具体的分享 token、文件名等敏感信息。
/// </summary>
public class ShareRiskDto
{
    /// <summary>
    /// 用户 ID（管理员查询时指定）。
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 该用户的分享总数。
    /// </summary>
    public int TotalShares { get; set; }

    /// <summary>
    /// 无过期时间的分享数量。
    /// </summary>
    public int NoExpirationCount { get; set; }

    /// <summary>
    /// 公开（无需密码）分享数量。
    /// </summary>
    public int PublicNoPasswordCount { get; set; }

    /// <summary>
    /// 长时间未使用（访问次数为 0）的分享数量。
    /// </summary>
    public int LongUnusedCount { get; set; }

    /// <summary>
    /// 无过期时间分享的提示文案。
    /// </summary>
    public string NoExpirationMessage { get; set; } = string.Empty;

    /// <summary>
    /// 公开分享的提示文案。
    /// </summary>
    public string PublicShareMessage { get; set; } = string.Empty;

    /// <summary>
    /// 长时间未使用的提示文案。
    /// </summary>
    public string UnusedShareMessage { get; set; } = string.Empty;
}
