using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore;

/// <summary>
/// 表示PrivateCloudDriveEntityFrameworkCoreCollection组件，封装对应业务场景的状态或行为。
/// </summary>
[CollectionDefinition(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class PrivateCloudDriveEntityFrameworkCoreCollection : ICollectionFixture<PrivateCloudDriveEntityFrameworkCoreFixture>
{

}
