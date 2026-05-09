using Volo.Abp.Modularity;

namespace PrivateCloudDrive;

/// <summary>
/// 表示PrivateCloudDriveApplicationTestBase组件，封装对应业务场景的状态或行为。
/// </summary>
public abstract class PrivateCloudDriveApplicationTestBase<TStartupModule> : PrivateCloudDriveTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
