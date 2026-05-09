namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 定义MobileAuthAuditLog相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class MobileAuthAuditLogConsts
{
    public const int MaxProviderLength = 32;
    public const int MaxActionLength = 64;
    public const int MaxResultLength = 32;
    public const int MaxFailureReasonLength = 512;
    public const int MaxClientIdLength = 64;
    public const int MaxUserNameLength = 256;
    public const int MaxDeviceIdHashLength = 128;
    public const int MaxUserAgentLength = 256;
}
