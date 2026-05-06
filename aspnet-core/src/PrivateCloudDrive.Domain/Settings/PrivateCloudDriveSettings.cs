namespace PrivateCloudDrive.Settings;

public static class PrivateCloudDriveSettings
{
    private const string Prefix = "PrivateCloudDrive";

    public static class FileCenter
    {
        private const string FileCenterPrefix = Prefix + ".FileCenter";

        public const string StorageRootPath = FileCenterPrefix + ".StorageRootPath";
        public const string MaxUploadFileSizeInBytes = FileCenterPrefix + ".MaxUploadFileSizeInBytes";
    }
}
