using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

/// <summary>
/// 配置PrivateCloudDriveApplicationTestModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveApplicationModule),
    typeof(PrivateCloudDriveDomainTestModule)
)]
public class PrivateCloudDriveApplicationTestModule : AbpModule
{

}
