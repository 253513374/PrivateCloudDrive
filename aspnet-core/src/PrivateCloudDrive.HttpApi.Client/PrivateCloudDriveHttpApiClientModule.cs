using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.VirtualFileSystem;

namespace PrivateCloudDrive;

/// <summary>
/// 配置PrivateCloudDriveHttpApiClientModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveApplicationContractsModule),
    typeof(AbpAccountHttpApiClientModule),
    typeof(AbpIdentityHttpApiClientModule),
    typeof(AbpPermissionManagementHttpApiClientModule),
    typeof(AbpTenantManagementHttpApiClientModule),
    typeof(AbpFeatureManagementHttpApiClientModule),
    typeof(AbpSettingManagementHttpApiClientModule)
)]
public class PrivateCloudDriveHttpApiClientModule : AbpModule
{
    public const string RemoteServiceName = "Default";

    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(PrivateCloudDriveApplicationContractsModule).Assembly,
            RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<PrivateCloudDriveHttpApiClientModule>();
        });
    }
}
