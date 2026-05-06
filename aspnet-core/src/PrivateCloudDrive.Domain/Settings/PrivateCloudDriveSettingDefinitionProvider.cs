using Volo.Abp.Settings;

namespace PrivateCloudDrive.Settings;

public class PrivateCloudDriveSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(PrivateCloudDriveSettings.FileCenter.StorageRootPath, "App_Data/FileCenter"),
            new SettingDefinition(PrivateCloudDriveSettings.FileCenter.MaxUploadFileSizeInBytes, "104857600"));
    }
}
