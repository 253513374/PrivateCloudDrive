namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 存储配置数据，映射后端 /api/admin/storage-config 的返回结构。
/// 包含存储后端类型、脱敏路径、总容量/已用/可用容量和单文件大小上限。
/// </summary>
public sealed record StorageConfigDto(
    string StorageProvider,
    string StoragePath,
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    long MaxSingleFileSize);
