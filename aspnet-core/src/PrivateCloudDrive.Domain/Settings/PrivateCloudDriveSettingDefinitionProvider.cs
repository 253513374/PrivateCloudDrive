using Volo.Abp.Settings;

namespace PrivateCloudDrive.Settings;

public class PrivateCloudDriveSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(PrivateCloudDriveSettings.MySetting1));
    }
}
