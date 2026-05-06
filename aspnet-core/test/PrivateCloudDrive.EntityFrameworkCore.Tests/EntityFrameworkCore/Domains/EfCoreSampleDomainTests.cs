using PrivateCloudDrive.Samples;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.Domains;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<PrivateCloudDriveEntityFrameworkCoreTestModule>
{

}
