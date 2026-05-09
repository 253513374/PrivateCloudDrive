namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 定义FileNode相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class FileNodeConsts
{
    public const int MaxNameLength = 255;
    public const int MaxNormalizedNameLength = 255;
    public const int MaxContentTypeLength = 256;
    public const int MaxBlobNameLength = 512;
}
