using System;
using System.Collections.Generic;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 部署就绪检查结果，供负载均衡器或编排平台判断系统是否可接收流量。
/// 仅返回低敏依赖状态（Pass/Warn/Fail），不含修复建议、物理路径、连接串详情。
/// </summary>
public class DeploymentReadyDto
{
    /// <summary>整体就绪状态：任一 Fail 则整体 Fail，全部 Pass 则 Pass，否则 Warn。</summary>
    public DeploymentCheckStatus OverallStatus { get; set; }

    /// <summary>各检查项低敏结果明细。</summary>
    public List<DeploymentReadyCheckDto> Checks { get; set; } = [];

    /// <summary>检查生成时间。</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>输出中是否包含敏感信息标记（保障永不包含）。</summary>
    public bool ContainsSensitiveData { get; set; } = false;
}

/// <summary>
/// 单次就绪检查结果，不含修复建议或敏感详情。
/// </summary>
public class DeploymentReadyCheckDto
{
    /// <summary>检查项名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>检查状态：Pass / Warn / Fail。</summary>
    public DeploymentCheckStatus Status { get; set; }

    /// <summary>安全就绪描述，不含敏感信息。</summary>
    public string Message { get; set; } = string.Empty;
}
