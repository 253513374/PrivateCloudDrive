using System;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 部署存活检查结果，仅用于确认进程存活和 API 可达性。
/// 不包含任何依赖状态或敏感信息。
/// </summary>
public class DeploymentLiveDto
{
    /// <summary>存活状态，始终为 "Healthy"。</summary>
    public string Status { get; set; } = "Healthy";

    /// <summary>检查生成时间。</summary>
    public DateTime GeneratedAt { get; set; }
}
