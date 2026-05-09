using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 配置PrivateCloudDriveFileCenterEntityFrameworkCoreModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainModule)
)]
public class PrivateCloudDriveFileCenterEntityFrameworkCoreModule : AbpModule
{
}
