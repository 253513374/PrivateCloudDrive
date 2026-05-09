using Volo.Abp.Account;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Microsoft.Extensions.DependencyInjection;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.MobileAuth;

namespace PrivateCloudDrive;

/// <summary>
/// 配置PrivateCloudDriveApplicationModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(PrivateCloudDriveApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(PrivateCloudDriveFileCenterApplicationModule)
    )]
public class PrivateCloudDriveApplicationModule : AbpModule
{
    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<MobileAuthLoginOptions>(configuration.GetSection("MobileAuth:LoginRateLimit"));
        Configure<WechatLoginOptions>(configuration.GetSection("Authentication:WeChat"));
        Configure<ExternalLoginOptions>(configuration.GetSection("Authentication:External"));

        context.Services.AddMapperlyObjectMapper<PrivateCloudDriveApplicationModule>();
    }
}
