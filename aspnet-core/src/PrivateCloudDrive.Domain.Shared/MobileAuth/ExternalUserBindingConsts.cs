namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 定义ExternalUserBinding相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class ExternalUserBindingConsts
{
    public const int MaxProviderLength = 32;
    public const int MaxProviderUserIdLength = 128;
    public const int MaxEmailLength = 256;
    public const int MaxDisplayNameLength = 128;
    public const int MaxAvatarUrlLength = 512;
}
