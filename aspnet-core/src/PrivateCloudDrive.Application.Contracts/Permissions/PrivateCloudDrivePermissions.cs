namespace PrivateCloudDrive.Permissions;

/// <summary>
/// 表示PrivateCloudDrivePermissions组件，封装对应业务场景的状态或行为。
/// </summary>
public static class PrivateCloudDrivePermissions
{
    public const string GroupName = "PrivateCloudDrive";

    /// <summary>
    /// 表示文件中心FileCenter，参与私有云盘文件、目录、分享、标签或媒体处理流程。
    /// </summary>
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

    /// <summary>
    /// 表示移动认证MobileAuth，参与第三方登录、账号绑定、审计或安全控制流程。
    /// </summary>
    public static class MobileAuth
    {
        public const string Default = GroupName + ".MobileAuth";
        public const string AuditLogs = Default + ".AuditLogs";
    }

    /// <summary>
    /// 表示OperationLogs组件，封装对应业务场景的状态或行为。
    /// </summary>
    public static class OperationLogs
    {
        public const string Default = GroupName + ".OperationLogs";
        public const string View = Default + ".View";
    }
}
