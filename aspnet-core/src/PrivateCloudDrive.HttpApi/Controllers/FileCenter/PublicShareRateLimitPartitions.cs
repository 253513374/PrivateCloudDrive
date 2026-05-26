using System;
using System.Security.Cryptography;
using System.Text;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 公开分享限流分区键生成器。
/// 分区键只使用分享 token 的 SHA-256 摘要，避免把原始 token 写入内存诊断、日志或指标标签。
/// </summary>
public static class PublicShareRateLimitPartitions
{
    public const string Global = "global";

    public static string ForTokenAndIp(string? token, string? clientIp)
    {
        return $"share:{HashToken(token)}:ip:{Normalize(clientIp, "unknown-ip")}";
    }

    public static string ForIp(string? clientIp)
    {
        return $"ip:{Normalize(clientIp, "unknown-ip")}";
    }

    private static string HashToken(string? token)
    {
        var normalizedToken = Normalize(token, "unknown-token");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
