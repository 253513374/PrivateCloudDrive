using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore;

[CollectionDefinition(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class PrivateCloudDriveEntityFrameworkCoreCollection : ICollectionFixture<PrivateCloudDriveEntityFrameworkCoreFixture>
{

}
