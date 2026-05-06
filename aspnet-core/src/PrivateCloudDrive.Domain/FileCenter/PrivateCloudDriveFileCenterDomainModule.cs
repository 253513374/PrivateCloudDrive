using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainSharedModule)
)]
public class PrivateCloudDriveFileCenterDomainModule : AbpModule
{
}
