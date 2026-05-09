using PrivateCloudDrive.Samples;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.Applications;

/// <summary>
/// 表示EfCoreSampleAppServiceTests组件，封装对应业务场景的状态或行为。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<PrivateCloudDriveEntityFrameworkCoreTestModule>
{

}
