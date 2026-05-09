using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

/// <summary>
/// 配置PrivateCloudDriveDomainTestModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveDomainModule),
    typeof(PrivateCloudDriveTestBaseModule)
)]
public class PrivateCloudDriveDomainTestModule : AbpModule
{

}
