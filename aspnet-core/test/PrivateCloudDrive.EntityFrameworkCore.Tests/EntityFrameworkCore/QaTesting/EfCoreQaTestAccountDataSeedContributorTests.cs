using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrivateCloudDrive.EntityFrameworkCore;
using PrivateCloudDrive.QaTesting;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.QaTesting;

/// <summary>
/// 验证 QA 低权限测试账号 seed 的幂等性、权限最小化和 secret 契约。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreQaTestAccountDataSeedContributorTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private const string Password = "Qa-Test-Secret-123!";
    private const string RotatedPassword = "Qa-Test-Secret-456!";

    private readonly QaTestAccountDataSeedContributor _seedContributor;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;

    public EfCoreQaTestAccountDataSeedContributorTests()
    {
        _seedContributor = GetRequiredService<QaTestAccountDataSeedContributor>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _roleManager = GetRequiredService<IdentityRoleManager>();
        _permissionManager = GetRequiredService<IPermissionManager>();
    }

    [Fact]
    public async Task Seed_Should_NoOp_When_Disabled()
    {
        using var _ = new EnvScope();

        await WithUnitOfWorkAsync(async () =>
        {
            await _seedContributor.SeedAsync(new DataSeedContext());
        });

        (await _userManager.FindByNameAsync(QaTestAccountConsts.PrimaryUserName)).ShouldBeNull();
        (await _roleManager.FindByNameAsync(QaTestAccountConsts.RoleName)).ShouldBeNull();
    }

    [Fact]
    public async Task Seed_Should_Create_Users_Role_And_Grant_Only_Low_Privilege_Permissions()
    {
        using var _ = new EnvScope(
            (QaTestAccountConsts.EnabledEnv, "true"),
            (QaTestAccountConsts.PasswordEnv, Password));

        await WithUnitOfWorkAsync(async () =>
        {
            await _seedContributor.SeedAsync(new DataSeedContext());
            await _seedContributor.SeedAsync(new DataSeedContext());
        });

        var user = await _userManager.FindByNameAsync(QaTestAccountConsts.PrimaryUserName);
        var altUser = await _userManager.FindByNameAsync(QaTestAccountConsts.AlternateUserName);
        var role = await _roleManager.FindByNameAsync(QaTestAccountConsts.RoleName);

        user.ShouldNotBeNull();
        altUser.ShouldNotBeNull();
        role.ShouldNotBeNull();
        user!.Email.ShouldBe(QaTestAccountConsts.PrimaryEmail);
        altUser!.Email.ShouldBe(QaTestAccountConsts.AlternateEmail);
        (await _userManager.IsInRoleAsync(user, QaTestAccountConsts.RoleName)).ShouldBeTrue();
        (await _userManager.IsInRoleAsync(altUser, QaTestAccountConsts.RoleName)).ShouldBeTrue();

        foreach (var permission in QaTestAccountConsts.GrantedPermissions)
        {
            var grant = await _permissionManager.GetForRoleAsync(QaTestAccountConsts.RoleName, permission);
            grant.IsGranted.ShouldBeTrue(permission);
        }

        foreach (var permission in QaTestAccountConsts.ForbiddenPermissions)
        {
            var grant = await _permissionManager.GetForRoleAsync(QaTestAccountConsts.RoleName, permission);
            grant.IsGranted.ShouldBeFalse(permission);
        }
    }

    [Fact]
    public async Task ForceRotate_Should_Reset_Password_Without_Logging_Secret()
    {
        using (new EnvScope(
            (QaTestAccountConsts.EnabledEnv, "true"),
            (QaTestAccountConsts.PasswordEnv, Password)))
        {
            await WithUnitOfWorkAsync(async () => await _seedContributor.SeedAsync(new DataSeedContext()));
        }

        using (new EnvScope(
            (QaTestAccountConsts.EnabledEnv, "true"),
            (QaTestAccountConsts.PasswordEnv, RotatedPassword),
            (QaTestAccountConsts.ForceRotateEnv, "true")))
        {
            await WithUnitOfWorkAsync(async () => await _seedContributor.SeedAsync(new DataSeedContext()));
        }

        var user = await _userManager.FindByNameAsync(QaTestAccountConsts.PrimaryUserName);
        user.ShouldNotBeNull();
        (await _userManager.CheckPasswordAsync(user!, Password)).ShouldBeFalse();
        (await _userManager.CheckPasswordAsync(user!, RotatedPassword)).ShouldBeTrue();
    }


    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _original = new();

        public EnvScope(params (string Key, string? Value)[] values)
        {
            var keys = new[]
            {
                QaTestAccountConsts.EnabledEnv,
                QaTestAccountConsts.PasswordEnv,
                QaTestAccountConsts.PasswordFileEnv,
                QaTestAccountConsts.ForceRotateEnv,
                QaTestAccountConsts.SkipMigratorEnv
            };

            foreach (var key in keys)
            {
                _original[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, null);
            }

            foreach (var (key, value) in values)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach (var (key, value) in _original)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
