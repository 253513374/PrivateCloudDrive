using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.MobileAuth;

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

    public void UpdateProfile(string? unionId, string? nickName, string? avatarUrl)
    {
        UnionId = Check.Length(Normalize(unionId), nameof(unionId), WechatUserBindingConsts.MaxUnionIdLength);
        NickName = Check.Length(Normalize(nickName), nameof(nickName), WechatUserBindingConsts.MaxNickNameLength);
        AvatarUrl = Check.Length(Normalize(avatarUrl), nameof(avatarUrl), WechatUserBindingConsts.MaxAvatarUrlLength);
    }

    public void MarkLogin(DateTime loginTime)
    {
        LastLoginTime = loginTime;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

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
