using PrivateCloudDrive.Samples;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.Domains;

/// <summary>
/// 表示EfCoreSampleDomainTests组件，封装对应业务场景的状态或行为。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<PrivateCloudDriveEntityFrameworkCoreTestModule>
{

}
