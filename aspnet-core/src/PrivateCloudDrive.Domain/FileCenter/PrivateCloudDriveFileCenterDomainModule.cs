using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 配置PrivateCloudDriveFileCenterDomainModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainSharedModule)
)]
public class PrivateCloudDriveFileCenterDomainModule : AbpModule
{
}
