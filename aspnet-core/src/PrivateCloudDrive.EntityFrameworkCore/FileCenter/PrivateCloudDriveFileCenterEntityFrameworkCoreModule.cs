using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainModule)
)]
public class PrivateCloudDriveFileCenterEntityFrameworkCoreModule : AbpModule
{
}
