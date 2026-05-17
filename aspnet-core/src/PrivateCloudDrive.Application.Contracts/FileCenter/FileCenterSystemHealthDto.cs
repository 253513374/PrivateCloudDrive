using System;
using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心系统健康状态，用于移动端设置页展示后端与存储可用性。
/// </summary>
public enum FileCenterSystemHealthStatus
{
    /// <summary>
    /// 状态健康，可正常使用。
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// 状态降级，核心能力可用但存在需要关注的风险。
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// 状态不可用，核心能力无法正常使用。
    /// </summary>
    Unhealthy = 2
}

/// <summary>
/// 文件中心系统健康摘要，不暴露存储密钥、物理路径等敏感信息。
/// </summary>
public class FileCenterSystemHealthDto
{
    /// <summary>
    /// 整体健康状态。
    /// </summary>
    public FileCenterSystemHealthStatus OverallStatus { get; set; }

    /// <summary>
    /// 后端 API 健康状态。
    /// </summary>
    public FileCenterSystemHealthStatus ApiStatus { get; set; }

    /// <summary>
    /// 数据库健康状态。
    /// </summary>
    public FileCenterSystemHealthStatus DatabaseStatus { get; set; }

    /// <summary>
    /// Redis/分布式缓存健康状态。
    /// </summary>
    public FileCenterSystemHealthStatus RedisStatus { get; set; }

    /// <summary>
    /// 存储后端健康状态。
    /// </summary>
    public FileCenterSystemHealthStatus StorageStatus { get; set; }

    /// <summary>
    /// FFmpeg 可用性配置状态。
    /// </summary>
    public FileCenterSystemHealthStatus FfmpegStatus { get; set; }

    /// <summary>
    /// FFprobe 可用性配置状态。
    /// </summary>
    public FileCenterSystemHealthStatus FfprobeStatus { get; set; }

    /// <summary>
    /// 当前配置的文件中心存储 Provider。
    /// </summary>
    public string StorageProvider { get; set; } = string.Empty;

    /// <summary>
    /// 面向移动端展示的存储位置说明，不包含服务器物理绝对路径或密钥。
    /// </summary>
    public string StorageLocationDescription { get; set; } = string.Empty;

    /// <summary>
    /// 面向用户展示的备份恢复边界说明。
    /// </summary>
    public string BackupScopeDescription { get; set; } = string.Empty;

    /// <summary>
    /// 面向用户展示的隐私与访问边界说明。
    /// </summary>
    public string PrivacyBoundaryDescription { get; set; } = string.Empty;

    /// <summary>
    /// 当前用户已使用容量。
    /// </summary>
    public long StorageUsedBytes { get; set; }

    /// <summary>
    /// 当前用户容量配额。
    /// </summary>
    public long StorageQuotaBytes { get; set; }

    /// <summary>
    /// 存储所在磁盘可用空间。对象存储或不可读取时为 0。
    /// </summary>
    public long StorageDiskAvailableBytes { get; set; }

    /// <summary>
    /// 存储所在磁盘总空间。对象存储或不可读取时为 0。
    /// </summary>
    public long StorageDiskTotalBytes { get; set; }

    /// <summary>
    /// 是否配置了有效容量配额。
    /// </summary>
    public bool IsQuotaConfigured { get; set; }

    /// <summary>
    /// 健康摘要生成时间。
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// 可安全展示给用户的诊断说明。
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; set; } = [];
}
