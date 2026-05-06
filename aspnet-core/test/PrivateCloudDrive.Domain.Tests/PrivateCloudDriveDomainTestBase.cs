using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

/* Inherit from this class for your domain layer tests. */
public abstract class PrivateCloudDriveDomainTestBase<TStartupModule> : PrivateCloudDriveTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
