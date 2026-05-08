using PrivateCloudDrive.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace PrivateCloudDrive.Permissions;

public class PrivateCloudDrivePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var privateCloudDriveGroup = context.AddGroup(
            PrivateCloudDrivePermissions.GroupName,
            L("Permission:PrivateCloudDrive"));

        var fileCenter = privateCloudDriveGroup.AddPermission(
            PrivateCloudDrivePermissions.FileCenter.Default,
            L("Permission:FileCenter"));

        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.View, L("Permission:FileCenter.View"));
        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.Upload, L("Permission:FileCenter.Upload"));
        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.Download, L("Permission:FileCenter.Download"));
        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.Delete, L("Permission:FileCenter.Delete"));
        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.Share, L("Permission:FileCenter.Share"));
        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.Tags, L("Permission:FileCenter.Tags"));
        fileCenter.AddChild(PrivateCloudDrivePermissions.FileCenter.Manage, L("Permission:FileCenter.Manage"));

        var mobileAuth = privateCloudDriveGroup.AddPermission(
            PrivateCloudDrivePermissions.MobileAuth.Default,
            L("Permission:MobileAuth"));

        mobileAuth.AddChild(
            PrivateCloudDrivePermissions.MobileAuth.AuditLogs,
            L("Permission:MobileAuth.AuditLogs"));

        var operationLogs = privateCloudDriveGroup.AddPermission(
            PrivateCloudDrivePermissions.OperationLogs.Default,
            L("Permission:OperationLogs"));

        operationLogs.AddChild(
            PrivateCloudDrivePermissions.OperationLogs.View,
            L("Permission:OperationLogs.View"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PrivateCloudDriveResource>(name);
    }
}
