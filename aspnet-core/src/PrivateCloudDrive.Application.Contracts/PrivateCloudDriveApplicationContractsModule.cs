using Volo.Abp.Account;
using PrivateCloudDrive.FileCenter;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace PrivateCloudDrive;

/// <summary>
/// 配置PrivateCloudDriveApplicationContractsModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveDomainSharedModule),
    typeof(AbpAccountApplicationContractsModule),
    typeof(AbpFeatureManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationContractsModule),
    typeof(AbpSettingManagementApplicationContractsModule),
    typeof(AbpTenantManagementApplicationContractsModule),
    typeof(AbpObjectExtendingModule),
    typeof(PrivateCloudDriveFileCenterApplicationContractsModule)
)]
public class PrivateCloudDriveApplicationContractsModule : AbpModule
{
    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PrivateCloudDriveDtoExtensions.Configure();
    }
}
