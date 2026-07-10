namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 管理员级别的存储配置展示 DTO（只读）。
/// </summary>
public class StorageConfigDto
{
    /// <summary>
    /// 存储后端类型（FileSystem / AliyunOss）。MinIO [计划支持]。
    /// </summary>
    public string StorageProvider { get; set; } = string.Empty;

    /// <summary>
    /// 存储路径（脱敏——不展示完整物理路径，仅展示相对路径或挂载点）。
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 总容量（字节）。
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 已用空间（字节）。
    /// </summary>
    public long UsedBytes { get; set; }

    /// <summary>
    /// 可用空间（字节）。
    /// </summary>
    public long AvailableBytes { get; set; }

    /// <summary>
    /// 单文件大小上限（字节）。0 表示未限制。
    /// </summary>
    public long MaxSingleFileSize { get; set; }
}
