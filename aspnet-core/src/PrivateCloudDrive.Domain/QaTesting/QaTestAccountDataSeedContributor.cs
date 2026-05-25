using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace PrivateCloudDrive.QaTesting;

/// <summary>
/// 按显式环境变量创建 QA 低权限测试账号。默认 no-op，避免开发/生产环境误建账号。
/// </summary>
public class QaTestAccountDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityRoleManager _roleManager;
    private readonly IdentityUserManager _userManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IPermissionManager _permissionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QaTestAccountDataSeedContributor> _logger;

    public QaTestAccountDataSeedContributor(
        IdentityRoleManager roleManager,
        IdentityUserManager userManager,
        IPermissionDataSeeder permissionDataSeeder,
        IPermissionManager permissionManager,
        IServiceProvider serviceProvider,
        ILogger<QaTestAccountDataSeedContributor> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionDataSeeder = permissionDataSeeder;
        _permissionManager = permissionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        var options = QaTestAccountSeedOptions.FromEnvironment();
        if (!options.Enabled)
        {
            _logger.LogInformation("QA test account seed skipped because {EnabledVariable} is not true.", QaTestAccountConsts.EnabledEnv);
            return;
        }

        await EnsureRoleAsync();
        await EnsureUserAsync(QaTestAccountConsts.PrimaryUserName, QaTestAccountConsts.PrimaryEmail, options);
        await EnsureUserAsync(QaTestAccountConsts.AlternateUserName, QaTestAccountConsts.AlternateEmail, options);

        _logger.LogInformation(
            "QA test account seed completed. user={UserName}; alt_user={AltUserName}; role={RoleName}; force_rotate={ForceRotate}",
            QaTestAccountConsts.PrimaryUserName,
            QaTestAccountConsts.AlternateUserName,
            QaTestAccountConsts.RoleName,
            options.ForceRotate);
    }

    private async Task EnsureRoleAsync()
    {
        var role = await _roleManager.FindByNameAsync(QaTestAccountConsts.RoleName);
        if (role == null)
        {
            role = new IdentityRole(Guid.NewGuid(), QaTestAccountConsts.RoleName);
            await CheckIdentityResultAsync(await _roleManager.CreateAsync(role));
        }

        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            QaTestAccountConsts.RoleName,
            QaTestAccountConsts.GrantedPermissions);

        foreach (var forbiddenPermission in QaTestAccountConsts.ForbiddenPermissions)
        {
            await _permissionManager.SetForRoleAsync(
                QaTestAccountConsts.RoleName,
                forbiddenPermission,
                isGranted: false);
        }
    }

    private async Task EnsureUserAsync(string userName, string email, QaTestAccountSeedOptions options)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new IdentityUser(Guid.NewGuid(), userName, email);
            user.SetEmailConfirmed(true);
            await CheckIdentityResultAsync(await _userManager.CreateAsync(user, options.Password!));
            await AddToQaRoleAsync(user);
            return;
        }

        if (user.Email != email)
        {
            await CheckIdentityResultAsync(await _userManager.SetEmailAsync(user, email));
        }

        if (!await _userManager.IsInRoleAsync(user, QaTestAccountConsts.RoleName))
        {
            await AddToQaRoleAsync(user);
        }

        if (options.ForceRotate)
        {
            await RotatePasswordAsync(user, options.Password!);
            await RevokeOpenIddictArtifactsAsync(user.Id.ToString());
        }
    }

    private async Task AddToQaRoleAsync(IdentityUser user)
    {
        await CheckIdentityResultAsync(await _userManager.AddToRoleAsync(user, QaTestAccountConsts.RoleName));
    }

    private async Task RotatePasswordAsync(IdentityUser user, string password)
    {
        if (await _userManager.HasPasswordAsync(user))
        {
            await CheckIdentityResultAsync(await _userManager.RemovePasswordAsync(user));
        }

        await CheckIdentityResultAsync(await _userManager.AddPasswordAsync(user, password));
        await CheckIdentityResultAsync(await _userManager.UpdateSecurityStampAsync(user));
    }

    private async Task RevokeOpenIddictArtifactsAsync(string subject)
    {
        var tokenManager = _serviceProvider.GetService<IOpenIddictTokenManager>();
        if (tokenManager != null)
        {
            await foreach (var token in tokenManager.FindBySubjectAsync(subject))
            {
                await tokenManager.TryRevokeAsync(token);
            }
        }

        var authorizationManager = _serviceProvider.GetService<IOpenIddictAuthorizationManager>();
        if (authorizationManager != null)
        {
            await foreach (var authorization in authorizationManager.FindBySubjectAsync(subject))
            {
                await authorizationManager.TryRevokeAsync(authorization);
            }
        }
    }

    private static Task CheckIdentityResultAsync(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return Task.CompletedTask;
        }

        throw new InvalidOperationException(
            "Failed to prepare QA test account: " + string.Join("; ", result.Errors.Select(error => error.Code)));
    }
}
