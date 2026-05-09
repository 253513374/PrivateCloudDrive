namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 定义FileShare相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class FileShareConsts
{
    public const int MaxTokenLength = 128;

    public const int MaxPasswordSaltLength = 64;

    public const int MaxPasswordHashLength = 128;
}
