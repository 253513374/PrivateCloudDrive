using System.Linq;
using System.Threading.Tasks;
using PrivateCloudDrive.Permissions;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Xunit;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心FileCenterPermissionDefinitionTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public abstract class FileCenterPermissionDefinitionTests<TStartupModule> : PrivateCloudDriveApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    protected FileCenterPermissionDefinitionTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Define_FileCenter_Permissions()
    {
        var permissionNames = (await _permissionDefinitionManager.GetPermissionsAsync())
            .Select(permission => permission.Name)
            .ToList();

        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.Default);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.View);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.Upload);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.Download);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.Delete);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.Share);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.FileCenter.Manage);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.MobileAuth.Default);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.MobileAuth.AuditLogs);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.OperationLogs.Default);
        permissionNames.ShouldContain(PrivateCloudDrivePermissions.OperationLogs.View);
    }
}
