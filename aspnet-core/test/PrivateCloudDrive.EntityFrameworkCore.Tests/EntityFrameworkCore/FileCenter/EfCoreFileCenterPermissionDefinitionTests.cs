using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterPermissionDefinitionTests
    : PrivateCloudDrive.FileCenter.FileCenterPermissionDefinitionTests<PrivateCloudDriveEntityFrameworkCoreTestModule>
{
}
