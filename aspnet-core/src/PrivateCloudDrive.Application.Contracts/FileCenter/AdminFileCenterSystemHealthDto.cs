using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 管理员级别的系统健康全局视图 DTO，包含版本号、总用户数和 PASS/WARN/FAIL 聚合。
/// </summary>
public class AdminFileCenterSystemHealthDto
{
    /// <summary>
    /// 整体健康等级：PASS / WARN / FAIL。
    /// </summary>
    public string OverallHealthLevel { get; set; } = "PASS";

    /// <summary>
    /// 系统版本号（AssemblyInformationalVersion）。
    /// </summary>
    public string SystemVersion { get; set; } = string.Empty;

    /// <summary>
    /// 注册用户总数。
    /// </summary>
    public long TotalUserCount { get; set; }

    /// <summary>
    /// 存储总容量（物理磁盘总空间）。
    /// </summary>
    public long TotalStorageBytes { get; set; }

    /// <summary>
    /// 总已使用存储。
    /// </summary>
    public long TotalUsedStorageBytes { get; set; }

    /// <summary>
    /// 基础健康摘要（继承现有 FileCenterSystemHealthDto 内容）。
    /// </summary>
    public FileCenterSystemHealthDto BaseHealthSummary { get; set; } = null!;
}
