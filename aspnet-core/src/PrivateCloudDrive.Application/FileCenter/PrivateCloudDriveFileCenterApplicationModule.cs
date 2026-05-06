using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainModule),
    typeof(PrivateCloudDriveFileCenterApplicationContractsModule)
)]
public class PrivateCloudDriveFileCenterApplicationModule : AbpModule
{
}
