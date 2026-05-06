using PrivateCloudDrive.Samples;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.Applications;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<PrivateCloudDriveEntityFrameworkCoreTestModule>
{

}
