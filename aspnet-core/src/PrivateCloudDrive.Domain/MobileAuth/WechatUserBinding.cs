using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证WechatUserBinding，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public class WechatUserBinding : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string AppId { get; private set; } = null!;

    public string OpenId { get; private set; } = null!;

    public string? UnionId { get; private set; }

    public string? NickName { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime? LastLoginTime { get; private set; }

    protected WechatUserBinding()
    {
    }

    /// <summary>
    /// 初始化 <see cref="WechatUserBinding"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public WechatUserBinding(
        Guid id,
        Guid? tenantId,
        Guid userId,
        string appId,
        string openId,
        string? unionId,
        string? nickName,
        string? avatarUrl)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        AppId = Check.Length(
            Check.NotNullOrWhiteSpace(appId, nameof(appId)),
            nameof(appId),
            WechatUserBindingConsts.MaxAppIdLength)!;
        OpenId = Check.Length(
            Check.NotNullOrWhiteSpace(openId, nameof(openId)),
            nameof(openId),
            WechatUserBindingConsts.MaxOpenIdLength)!;
        IsEnabled = true;

        UpdateProfile(unionId, nickName, avatarUrl);
    }

    /// <summary>
    /// 更新现有业务资源，并保持跨层数据和领域状态一致。
    /// </summary>
    public void UpdateProfile(string? unionId, string? nickName, string? avatarUrl)
    {
        UnionId = Check.Length(Normalize(unionId), nameof(unionId), WechatUserBindingConsts.MaxUnionIdLength);
        NickName = Check.Length(Normalize(nickName), nameof(nickName), WechatUserBindingConsts.MaxNickNameLength);
        AvatarUrl = Check.Length(Normalize(avatarUrl), nameof(avatarUrl), WechatUserBindingConsts.MaxAvatarUrlLength);
    }

    /// <summary>
    /// 执行MarkLogin操作，封装该场景下的业务规则、异常处理和结果返回。
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
    /// 执行Disable操作，封装该场景下的业务规则、异常处理和结果返回。
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
