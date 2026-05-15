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
    /// 存储后端健康状态。
    /// </summary>
    public FileCenterSystemHealthStatus StorageStatus { get; set; }

    /// <summary>
    /// 当前配置的文件中心存储 Provider。
    /// </summary>
    public string StorageProvider { get; set; } = string.Empty;

    /// <summary>
    /// 当前用户已使用容量。
    /// </summary>
    public long StorageUsedBytes { get; set; }

    /// <summary>
    /// 当前用户容量配额。
    /// </summary>
    public long StorageQuotaBytes { get; set; }

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
