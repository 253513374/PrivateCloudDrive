using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

[DependsOn(
    typeof(PrivateCloudDriveApplicationModule),
    typeof(PrivateCloudDriveDomainTestModule)
)]
public class PrivateCloudDriveApplicationTestModule : AbpModule
{

}
