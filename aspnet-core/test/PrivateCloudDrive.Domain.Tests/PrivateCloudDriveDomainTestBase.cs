using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

/* Inherit from this class for your domain layer tests. */
/// <summary>
/// 表示PrivateCloudDriveDomainTestBase组件，封装对应业务场景的状态或行为。
/// </summary>
public abstract class PrivateCloudDriveDomainTestBase<TStartupModule> : PrivateCloudDriveTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
