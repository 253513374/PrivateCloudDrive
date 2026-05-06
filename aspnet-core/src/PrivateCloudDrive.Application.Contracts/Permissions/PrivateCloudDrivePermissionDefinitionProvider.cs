using PrivateCloudDrive.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace PrivateCloudDrive.Permissions;

public class PrivateCloudDrivePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(PrivateCloudDrivePermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(PrivateCloudDrivePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PrivateCloudDriveResource>(name);
    }
}
