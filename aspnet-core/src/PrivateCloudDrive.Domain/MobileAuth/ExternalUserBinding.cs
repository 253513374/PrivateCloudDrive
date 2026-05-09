using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// PrivateCloudDrive 用户与第三方登录身份之间的绑定关系。
/// 绑定只保存 Provider 用户标识和展示资料，不保存授权码、access token 或 refresh token。
/// </summary>
public class ExternalUserBinding : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string Provider { get; private set; } = null!;

    public string ProviderUserId { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime? LastLoginTime { get; private set; }

    protected ExternalUserBinding()
    {
    }

    /// <summary>
    /// 创建一条启用状态的第三方账号绑定。
    /// </summary>
    public ExternalUserBinding(
        Guid id,
        Guid? tenantId,
        Guid userId,
        string provider,
        string providerUserId,
        string? email,
        string? displayName,
        string? avatarUrl)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Provider = Check.Length(
            Check.NotNullOrWhiteSpace(provider, nameof(provider)),
            nameof(provider),
            ExternalUserBindingConsts.MaxProviderLength)!;
        ProviderUserId = Check.Length(
            Check.NotNullOrWhiteSpace(providerUserId, nameof(providerUserId)),
            nameof(providerUserId),
            ExternalUserBindingConsts.MaxProviderUserIdLength)!;
        IsEnabled = true;

        UpdateProfile(email, displayName, avatarUrl);
    }

    /// <summary>
    /// 用 Provider 最新返回的公开资料刷新展示信息。
    /// </summary>
    public void UpdateProfile(string? email, string? displayName, string? avatarUrl)
    {
        Email = Check.Length(Normalize(email), nameof(email), ExternalUserBindingConsts.MaxEmailLength);
        DisplayName = Check.Length(Normalize(displayName), nameof(displayName), ExternalUserBindingConsts.MaxDisplayNameLength);
        AvatarUrl = Check.Length(Normalize(avatarUrl), nameof(avatarUrl), ExternalUserBindingConsts.MaxAvatarUrlLength);
    }

    /// <summary>
    /// 记录第三方登录成功时间，用于设置页展示和安全审计辅助判断。
    /// </summary>
    public void MarkLogin(DateTime loginTime)
    {
        LastLoginTime = loginTime;
    }

    /// <summary>
    /// 执行Enable操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// 软解绑第三方账号，保留历史记录供审计追踪。
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
