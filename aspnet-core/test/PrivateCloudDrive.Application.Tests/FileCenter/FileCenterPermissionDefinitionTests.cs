using System.Linq;
using System.Threading.Tasks;
using PrivateCloudDrive.Permissions;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Xunit;

namespace PrivateCloudDrive.FileCenter;

public abstract class FileCenterPermissionDefinitionTests<TStartupModule> : PrivateCloudDriveApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    protected FileCenterPermissionDefinitionTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

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
    }
}
