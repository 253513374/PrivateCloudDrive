namespace PrivateCloudDrive.QaTesting;

/// <summary>
/// QA 低权限测试账号 seed 的固定契约，避免脚本、CI 和测试代码各自散落魔法字符串。
/// </summary>
public static class QaTestAccountConsts
{
    public const string RoleName = "QA.Tester";
    public const string PrimaryUserName = "qa_user";
    public const string AlternateUserName = "qa_user_alt";
    public const string PrimaryEmail = "qa_user@privateclouddrive.local";
    public const string AlternateEmail = "qa_user_alt@privateclouddrive.local";

    public const string EnabledEnv = "PCD_QA_TEST_ACCOUNT_ENABLED";
    public const string PasswordEnv = "PCD_QA_TEST_ACCOUNT_PASSWORD";
    public const string PasswordFileEnv = "PCD_QA_TEST_ACCOUNT_PASSWORD_FILE";
    public const string ForceRotateEnv = "PCD_QA_TEST_ACCOUNT_FORCE_ROTATE";
    public const string SkipMigratorEnv = "PCD_QA_TEST_ACCOUNT_SKIP_MIGRATOR";

    public const int MinimumPasswordLength = 8;

    public static readonly string[] GrantedPermissions =
    [
        "PrivateCloudDrive.FileCenter.View",
        "PrivateCloudDrive.FileCenter.Upload",
        "PrivateCloudDrive.FileCenter.Download",
        "PrivateCloudDrive.FileCenter.Delete",
        "PrivateCloudDrive.FileCenter.Share",
        "PrivateCloudDrive.FileCenter.Tags"
    ];

    public static readonly string[] ForbiddenPermissions =
    [
        "PrivateCloudDrive.FileCenter.Manage",
        "PrivateCloudDrive.MobileAuth.AuditLogs",
        "PrivateCloudDrive.OperationLogs.View"
    ];
}
