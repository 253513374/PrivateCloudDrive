namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 定义WechatUserBinding相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class WechatUserBindingConsts
{
    public const int MaxAppIdLength = 64;
    public const int MaxOpenIdLength = 128;
    public const int MaxUnionIdLength = 128;
    public const int MaxNickNameLength = 128;
    public const int MaxAvatarUrlLength = 512;
    public const int MaxPlatformLength = 32;
}
