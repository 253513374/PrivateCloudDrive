namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 定义BlobObject相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class BlobObjectConsts
{
    public const int MaxBlobNameLength = 512;
    public const int MaxFileNameLength = 255;
    public const int MaxContentTypeLength = 256;
    public const int MaxHashLength = 128;
}
