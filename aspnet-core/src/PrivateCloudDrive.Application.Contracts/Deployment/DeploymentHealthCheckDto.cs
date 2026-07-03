using System;
using System.Collections.Generic;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 部署健康检查项状态级别，确保非开发者在部署后可通过单条命令或 API 调用确认系统就绪。
/// </summary>
public enum DeploymentCheckStatus
{
    /// <summary>检查通过，组件正常。</summary>
    Pass = 0,

    /// <summary>检查降级，核心能力可用但存在需要关注的风险。</summary>
    Warn = 1,

    /// <summary>检查失败，核心能力不可用需要立即修复。</summary>
    Fail = 2
}

/// <summary>
/// 单次部署健康检查结果，不暴露密码、token、OAuth code、client secret 或完整私有 URL。
/// </summary>
public class DeploymentCheckResultDto
{
    /// <summary>检查项名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>检查状态：Pass / Warn / Fail。</summary>
    public DeploymentCheckStatus Status { get; set; }

    /// <summary>面向运维人员的安全检查描述，不含敏感信息。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>仅当 Status 为 Fail 时提供修复建议。</summary>
    public string? FixSuggestion { get; set; }
}

/// <summary>
/// 部署健康检查整体结果，供运维人员单条命令确认系统就绪。
/// </summary>
public class DeploymentHealthDto
{
    /// <summary>整体健康状态：任一 Fail 则整体 Fail，全部 Pass 则 Pass，否则 Warn。</summary>
    public DeploymentCheckStatus OverallStatus { get; set; }

    /// <summary>各检查项结果明细。</summary>
    public List<DeploymentCheckResultDto> Checks { get; set; } = [];

    /// <summary>检查生成时间。</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>输出中是否包含敏感信息标记（本端点保障永不包含）。</summary>
    public bool ContainsSensitiveData { get; set; } = false;
}
