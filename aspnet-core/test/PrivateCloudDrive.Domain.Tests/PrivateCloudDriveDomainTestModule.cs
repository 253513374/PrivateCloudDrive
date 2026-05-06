using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

[DependsOn(
    typeof(PrivateCloudDriveDomainModule),
    typeof(PrivateCloudDriveTestBaseModule)
)]
public class PrivateCloudDriveDomainTestModule : AbpModule
{

}
