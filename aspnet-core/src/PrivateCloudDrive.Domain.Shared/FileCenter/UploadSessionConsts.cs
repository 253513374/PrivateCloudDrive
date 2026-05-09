namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 定义UploadSession相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class UploadSessionConsts
{
    public const int MaxFileNameLength = 255;
    public const int MaxNormalizedFileNameLength = 255;
    public const int MaxContentTypeLength = 256;
    public const int MaxSha256Length = 64;
    public const int MaxUploadedChunksJsonLength = 16384;
}
