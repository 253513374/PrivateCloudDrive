using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

public abstract class PrivateCloudDriveApplicationTestBase<TStartupModule> : PrivateCloudDriveTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
