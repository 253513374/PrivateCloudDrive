namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 当前用户文件中心容量使用摘要。
/// </summary>
public class StorageUsageDto
{
    /// <summary>
    /// 当前用户已使用的存储字节数。
    /// </summary>
    public long UsedBytes { get; set; }

    /// <summary>
    /// 当前用户可用的配额字节数。
    /// </summary>
    public long QuotaBytes { get; set; }

    /// <summary>
    /// 当前用户剩余的配额字节数；已超额时为 0。
    /// </summary>
    public long RemainingBytes { get; set; }

    /// <summary>
    /// 容量使用百分比，保留两位小数。
    /// </summary>
    public decimal UsagePercent { get; set; }

    /// <summary>
    /// 是否已配置有效配额。
    /// </summary>
    public bool IsQuotaConfigured { get; set; }

    /// <summary>
    /// 单文件上传大小上限（字节）。0 表示未限制。
    /// </summary>
    public long MaxSingleFileSize { get; set; }
}
