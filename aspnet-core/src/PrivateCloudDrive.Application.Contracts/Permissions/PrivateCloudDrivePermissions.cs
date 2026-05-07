namespace PrivateCloudDrive.Permissions;

public static class PrivateCloudDrivePermissions
{
    public const string GroupName = "PrivateCloudDrive";

    public static class FileCenter
    {
        public const string Default = GroupName + ".FileCenter";
        public const string View = Default + ".View";
        public const string Upload = Default + ".Upload";
        public const string Download = Default + ".Download";
        public const string Delete = Default + ".Delete";
        public const string Share = Default + ".Share";
        public const string Tags = Default + ".Tags";
        public const string Manage = Default + ".Manage";
    }
}
