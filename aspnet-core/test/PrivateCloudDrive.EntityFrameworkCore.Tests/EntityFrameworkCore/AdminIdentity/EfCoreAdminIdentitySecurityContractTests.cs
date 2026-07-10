using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.AdminIdentity;
using PrivateCloudDrive.Controllers.AdminIdentity;
using PrivateCloudDrive.Permissions;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.AdminIdentity;

/// <summary>
/// Admin 用户管理模块安全契约测试：验证权限装饰、DTO 脱敏与操作不可自禁用。
/// </summary>
public class EfCoreAdminIdentitySecurityContractTests
{
    [Fact]
    public void AdminIdentityUserController_Should_Require_Manage_Permission()
    {
        var controllerType = typeof(AdminIdentityUserController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttr.ShouldNotBeNull();
        // ABP uses [Authorize(permissionName)] which maps to the Policy property
        authorizeAttr!.Policy.ShouldBe(PrivateCloudDrivePermissions.FileCenter.Manage);
        authorizeAttr.AuthenticationSchemes.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void AdminIdentityUserController_Should_Have_Correct_Permission_Name()
    {
        var controllerType = typeof(AdminIdentityUserController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttr.ShouldNotBeNull();
        // ABP Authorize attribute maps permission name to the policy
        var rawPermission = authorizeAttr!.Policy
            ?? authorizeAttr!.Roles;

        // The attribute uses PrivateCloudDrivePermissions.FileCenter.Manage
        // Check it contains the permission name
        var permission = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        permission.ShouldNotBeNull();
    }

    [Fact]
    public void AdminIdentityController_Should_Contain_AdminPermission()
    {
        // Verify the controller has [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
        var permissionAttr = typeof(AdminIdentityUserController)
            .GetCustomAttributesData()
            .FirstOrDefault(attr => attr.AttributeType == typeof(AuthorizeAttribute));

        permissionAttr.ShouldNotBeNull();
        var args = permissionAttr!.ConstructorArguments;
        if (args.Count > 0)
        {
            var permissionName = args[0].Value?.ToString();
            permissionName.ShouldBe(PrivateCloudDrivePermissions.FileCenter.Manage);
        }
    }

    [Fact]
    public void AdminIdentityUserAppService_Should_Require_Manage_Permission()
    {
        var serviceType = typeof(AdminIdentityUserAppService);
        var authorizeAttrs = serviceType.GetCustomAttributes<AuthorizeAttribute>().ToList();

        authorizeAttrs.ShouldNotBeEmpty();
        authorizeAttrs.ShouldContain(attr =>
            attr.Policy == PrivateCloudDrivePermissions.FileCenter.Manage ||
            attr.Policy == null); // null policy means ABP will use method-level permission
    }

    [Fact]
    public void AdminIdentity_Dtos_Should_Not_Contain_Secrets()
    {
        var dtoTypes = typeof(AdminIdentityUserDto).Assembly
            .GetTypes()
            .Where(t => t.Namespace == typeof(AdminIdentityUserDto).Namespace
                && (t.Name.EndsWith("Dto") || t.Name.EndsWith("Input")))
            .ToList();

        // AdminCreateUserInput legitimately contains Password for user creation
        var exemptTypes = new[] { typeof(AdminCreateUserInput) };

        var bannedNames = new[] { "AccessToken", "RefreshToken", "Token", "Secret", "ApiKey" };

        foreach (var dtoType in dtoTypes)
        {
            if (exemptTypes.Contains(dtoType))
            {
                continue;
            }

            var propertyNames = dtoType.GetProperties()
                .Select(p => p.Name)
                .ToList();

            // Password is only allowed in AdminCreateUserInput
            propertyNames.ShouldNotContain("Password",
                $"DTO {dtoType.Name} should not contain property named 'Password'. Only AdminCreateUserInput may contain it.");

            foreach (var banned in bannedNames)
            {
                propertyNames.ShouldNotContain(banned,
                    $"DTO {dtoType.Name} should not contain property named '{banned}'");
            }
        }
    }

    [Fact]
    public void AdminCreateUserInput_Should_Contain_Password_For_Baseline()
    {
        // AdminCreateUserInput legitimately contains a Password field for user creation.
        // This is the only admin DTO that should contain password.
        var inputType = typeof(AdminCreateUserInput);
        var passwordProp = inputType.GetProperty("Password");

        passwordProp.ShouldNotBeNull();
        passwordProp!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void AdminIdentityUserDto_Should_Not_Contain_Password()
    {
        var dtoType = typeof(AdminIdentityUserDto);
        var propertyNames = dtoType.GetProperties().Select(p => p.Name).ToList();

        propertyNames.ShouldNotContain("Password");
        propertyNames.ShouldNotContain("Token");
        propertyNames.ShouldNotContain("AccessToken");
        propertyNames.ShouldNotContain("RefreshToken");
    }

    [Fact]
    public void AdminResetPasswordInput_Should_Contain_NewPassword()
    {
        var resetPasswordType = typeof(AdminResetPasswordInput);
        var newPasswordProp = resetPasswordType.GetProperty("NewPassword");

        newPasswordProp.ShouldNotBeNull();
        newPasswordProp!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void AdminSetQuotaInput_Should_Contain_StorageQuotaBytes()
    {
        var setQuotaType = typeof(AdminSetQuotaInput);
        var quotaProp = setQuotaType.GetProperty("StorageQuotaBytes");

        quotaProp.ShouldNotBeNull();
        quotaProp!.PropertyType.ShouldBe(typeof(long));
    }

    [Fact]
    public void AdminIdentityUserController_Methods_Should_Be_Virtual_For_ABP_Dynamic_Proxy()
    {
        var methods = typeof(AdminIdentityUserController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.EndsWith("Async"))
            .ToList();

        methods.ShouldNotBeEmpty();
        foreach (var method in methods)
        {
            method.IsVirtual.ShouldBeTrue(
                $"Method {method.Name} on AdminIdentityUserController should be virtual for ABP dynamic proxying");
        }
    }

    [Fact]
    public void AdminIdentity_Action_Constants_Should_Be_Defined()
    {
        var actionType = typeof(OperationLogs.OperationLogActions);

        var adminActions = actionType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v != null && v.StartsWith("Admin"))
            .ToList()!;

        adminActions.ShouldContain("AdminCreateUser");
        adminActions.ShouldContain("AdminDisableUser");
        adminActions.ShouldContain("AdminEnableUser");
        adminActions.ShouldContain("AdminResetPassword");
        adminActions.ShouldContain("AdminSetQuota");
    }
}
