namespace PrivateCloudDrive.Settings;

/// <summary>
/// 表示PrivateCloudDriveSettings组件，封装对应业务场景的状态或行为。
/// </summary>
public static class PrivateCloudDriveSettings
{
    private const string Prefix = "PrivateCloudDrive";

    /// <summary>
    /// 表示文件中心FileCenter，参与私有云盘文件、目录、分享、标签或媒体处理流程。
    /// </summary>
    public static class FileCenter
    {
        private const string FileCenterPrefix = Prefix + ".FileCenter";

        public const string StorageRootPath = FileCenterPrefix + ".StorageRootPath";
        public const string MaxUploadFileSizeInBytes = FileCenterPrefix + ".MaxUploadFileSizeInBytes";
        public const string UserStorageQuotaInBytes = FileCenterPrefix + ".UserStorageQuotaInBytes";
    }
}
