using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心EfCoreFileCenterPermissionDefinitionTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterPermissionDefinitionTests
    : PrivateCloudDrive.FileCenter.FileCenterPermissionDefinitionTests<PrivateCloudDriveEntityFrameworkCoreTestModule>
{
}
