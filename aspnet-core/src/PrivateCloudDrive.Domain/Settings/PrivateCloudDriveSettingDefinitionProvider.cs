using Volo.Abp.Settings;

namespace PrivateCloudDrive.Settings;

/// <summary>
/// 表示PrivateCloudDriveSettingDefinitionProvider组件，封装对应业务场景的状态或行为。
/// </summary>
public class PrivateCloudDriveSettingDefinitionProvider : SettingDefinitionProvider
{
    /// <summary>
    /// 执行Define操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(PrivateCloudDriveSettings.FileCenter.StorageRootPath, "App_Data/FileCenter"),
            new SettingDefinition(PrivateCloudDriveSettings.FileCenter.MaxUploadFileSizeInBytes, "104857600"),
            new SettingDefinition(PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes, "10737418240"));
    }
}
