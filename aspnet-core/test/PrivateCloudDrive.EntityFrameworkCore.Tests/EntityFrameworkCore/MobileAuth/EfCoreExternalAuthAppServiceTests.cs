using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrivateCloudDrive.EntityFrameworkCore;
using PrivateCloudDrive.Permissions;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证EfCoreExternalAuthAppServiceTests，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreExternalAuthAppServiceTests : PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private const string TestPassword = "P@ssw0rd1";

    private readonly IExternalLoginService _externalLoginService;
    private readonly IExternalAuthAppService _externalAuthAppService;
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<ExternalUserBinding, Guid> _bindingRepository;
    private readonly IRepository<MobileAuthAuditLog, Guid> _auditLogRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 初始化 <see cref="EfCoreExternalAuthAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreExternalAuthAppServiceTests()
    {
        _externalLoginService = GetRequiredService<IExternalLoginService>();
        _externalAuthAppService = GetRequiredService<IExternalAuthAppService>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _bindingRepository = GetRequiredService<IRepository<ExternalUserBinding, Guid>>();
        _auditLogRepository = GetRequiredService<IRepository<MobileAuthAuditLog, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _permissionChecker = GetRequiredService<IPermissionChecker>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Return_Safe_External_Login_Settings()
    {
        var settings = await _externalAuthAppService.GetSettingsAsync();

        settings.Providers.Count.ShouldBe(2);
        settings.Providers.ShouldContain(provider =>
            provider.Provider == ExternalLoginConsts.GoogleProviderName &&
            provider.IsEnabled &&
            provider.ClientId == "google-test-client" &&
            provider.UsePkce);
        settings.Providers.ShouldContain(provider =>
            provider.Provider == ExternalLoginConsts.GitHubProviderName &&
            provider.IsEnabled &&
            provider.ClientId == "github-test-client" &&
            provider.UsePkce);

        var propertyNames = typeof(ExternalLoginProviderSettingsDto)
            .GetProperties()
            .Concat(typeof(ExternalBindingDto).GetProperties())
            .Select(property => property.Name)
            .ToList();

        propertyNames.ShouldNotContain("ClientSecret");
        propertyNames.ShouldNotContain("ProviderUserId");
        propertyNames.ShouldNotContain("AccessToken");
        propertyNames.ShouldNotContain("RefreshToken");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Auto_Provision_User_And_Binding_When_Google_Login_Is_Not_Bound()
    {
        var result = await _externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = ExternalLoginConsts.GoogleProviderName,
            Code = TestExternalIdentityService.GoogleAliceCode,
            RedirectUri = "privateclouddrive://callback",
            CodeVerifier = "google-code-verifier",
            ClientId = "PrivateCloudDrive_App",
            DeviceIdHash = "device-hash-1"
        });

        result.Succeeded.ShouldBeTrue();
        result.BindingTicket.ShouldBeNull();
        result.UserId.ShouldNotBeNull();

        var user = await _userManager.FindByIdAsync(result.UserId!.Value.ToString());
        user.ShouldNotBeNull();
        user!.UserName.ShouldStartWith("google-alice-");
        user.Email.ShouldBe("alice@example.test");
        user.EmailConfirmed.ShouldBeTrue();
        user.PasswordHash.ShouldNotBeNullOrWhiteSpace();

        await WithCurrentUserAsync(user, async () =>
        {
            (await _permissionChecker.IsGrantedAsync(PrivateCloudDrivePermissions.FileCenter.View)).ShouldBeTrue();
            (await _permissionChecker.IsGrantedAsync(PrivateCloudDrivePermissions.FileCenter.Upload)).ShouldBeTrue();
            (await _permissionChecker.IsGrantedAsync(PrivateCloudDrivePermissions.FileCenter.Download)).ShouldBeTrue();
            (await _permissionChecker.IsGrantedAsync(PrivateCloudDrivePermissions.FileCenter.Delete)).ShouldBeTrue();
            (await _permissionChecker.IsGrantedAsync(PrivateCloudDrivePermissions.FileCenter.Manage)).ShouldBeTrue();
        });

        var binding = (await _bindingRepository.GetListAsync())
            .Single(item => item.UserId == user.Id);
        binding.Provider.ShouldBe(ExternalLoginConsts.GoogleProviderName);
        binding.Email.ShouldBe("alice@example.test");
        binding.DisplayName.ShouldBe("Alice");
        binding.IsEnabled.ShouldBeTrue();
        binding.LastLoginTime.ShouldNotBeNull();

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.Google &&
            log.Action == MobileAuthAuditLogActions.ExternalBind &&
            log.Result == MobileAuthAuditLogResults.Success &&
            log.FailureReason == "auto_provisioned" &&
            log.UserId == user.Id &&
            log.ClientId == "PrivateCloudDrive_App" &&
            log.DeviceIdHash == "device-hash-1");
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.Google &&
            log.Action == MobileAuthAuditLogActions.ExternalLogin &&
            log.Result == MobileAuthAuditLogResults.Success &&
            log.UserId == user.Id &&
            log.ClientId == "PrivateCloudDrive_App" &&
            log.DeviceIdHash == "device-hash-1");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Login_With_Existing_Auto_Provisioned_Google_Binding()
    {
        var firstLogin = await _externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = ExternalLoginConsts.GoogleProviderName,
            Code = TestExternalIdentityService.GoogleAliceCode,
            RedirectUri = "privateclouddrive://callback"
        });

        firstLogin.Succeeded.ShouldBeTrue();
        var userId = firstLogin.UserId!.Value;

        var secondLogin = await _externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = ExternalLoginConsts.GoogleProviderName,
            Code = TestExternalIdentityService.GoogleAliceUpdatedCode,
            RedirectUri = "privateclouddrive://callback"
        });

        secondLogin.Succeeded.ShouldBeTrue();
        secondLogin.UserId.ShouldBe(userId);

        var binding = (await _bindingRepository.GetListAsync())
            .Single(item => item.UserId == userId);
        binding.DisplayName.ShouldBe("Alice Updated");
        binding.IsEnabled.ShouldBeTrue();
        binding.LastLoginTime.ShouldNotBeNull();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        user.ShouldNotBeNull();

        await WithCurrentUserAsync(user, async () =>
        {
            var bindings = await _externalAuthAppService.GetBindingsAsync();
            bindings.Count.ShouldBe(1);
            bindings[0].Provider.ShouldBe(ExternalLoginConsts.GoogleProviderName);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Prevent_External_Binding_From_Moving_To_Another_User()
    {
        var otherUser = await CreateUserAsync("external-other", "external-other@example.test");
        var firstLogin = await _externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = ExternalLoginConsts.GitHubProviderName,
            Code = TestExternalIdentityService.GitHubBobCode,
            RedirectUri = "privateclouddrive://callback"
        });

        firstLogin.Succeeded.ShouldBeTrue();

        await WithCurrentUserAsync(otherUser, async () =>
        {
            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _externalAuthAppService.BindCurrentAsync(new BindCurrentExternalLoginInput
                {
                    Provider = ExternalLoginConsts.GitHubProviderName,
                    Code = TestExternalIdentityService.GitHubBobCode,
                    RedirectUri = "privateclouddrive://callback",
                    DeviceIdHash = "device-hash-2"
                });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.ExternalLoginAlreadyBound);
        });

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.GitHub &&
            log.Action == MobileAuthAuditLogActions.ExternalBind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == ExternalLoginConsts.AlreadyBoundError &&
            log.UserId == otherUser.Id &&
            log.DeviceIdHash == "device-hash-2");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Unbind_Current_User_Without_Removing_Password_Login()
    {
        var firstLogin = await _externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = ExternalLoginConsts.GitHubProviderName,
            Code = TestExternalIdentityService.GitHubBobCode,
            RedirectUri = "privateclouddrive://callback"
        });

        firstLogin.Succeeded.ShouldBeTrue();
        var user = await _userManager.FindByIdAsync(firstLogin.UserId!.Value.ToString());
        user.ShouldNotBeNull();

        await WithCurrentUserAsync(user!, async () =>
        {
            await _externalAuthAppService.UnbindAsync(ExternalLoginConsts.GitHubProviderName);
            var bindings = await _externalAuthAppService.GetBindingsAsync();
            bindings.ShouldBeEmpty();
        });

        var binding = (await _bindingRepository.GetListAsync())
            .Single(item => item.UserId == user!.Id);
        binding.IsEnabled.ShouldBeFalse();

        var storedUser = await _userManager.FindByIdAsync(user!.Id.ToString());
        storedUser.ShouldNotBeNull();
        storedUser!.PasswordHash.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Record_Safe_External_Exchange_Failure_Without_Secrets()
    {
        var result = await _externalLoginService.LoginAsync(new ExternalLoginInput
        {
            Provider = ExternalLoginConsts.GoogleProviderName,
            Code = TestExternalIdentityService.FailedCode,
            RedirectUri = "privateclouddrive://callback",
            ClientId = "PrivateCloudDrive_App"
        });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(ExternalLoginConsts.CodeExchangeFailedError);

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.Google &&
            log.Action == MobileAuthAuditLogActions.ExternalLogin &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason != null &&
            log.FailureReason.StartsWith(ExternalLoginConsts.CodeExchangeFailedError, StringComparison.Ordinal));
        auditLogs.ShouldNotContain(log =>
            log.FailureReason != null &&
            log.FailureReason.Contains(TestExternalIdentityService.FailedCode, StringComparison.Ordinal));
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public void Should_Use_Postgres_Safe_Unique_Indexes_For_External_Bindings()
    {
        var dbContext = GetRequiredService<PrivateCloudDriveDbContext>();
        var entityType = dbContext.Model.FindEntityType(typeof(ExternalUserBinding));
        entityType.ShouldNotBeNull();

        var indexes = entityType!.GetIndexes()
            .ToDictionary(index => index.GetDatabaseName()!);

        AssertIndex(
            indexes,
            "UX_ExternalUserBindings_Host_Provider_UserId",
            new[] { nameof(ExternalUserBinding.Provider), nameof(ExternalUserBinding.ProviderUserId) },
            "\"TenantId\" IS NULL");

        AssertIndex(
            indexes,
            "UX_ExternalUserBindings_Tenant_Provider_UserId",
            new[] { nameof(ExternalUserBinding.TenantId), nameof(ExternalUserBinding.Provider), nameof(ExternalUserBinding.ProviderUserId) },
            "\"TenantId\" IS NOT NULL");
    }

    private async Task<IdentityUser> CreateUserAsync(string userName, string email)
    {
        var user = new IdentityUser(Guid.NewGuid(), userName, email);
        var result = await _userManager.CreateAsync(user, TestPassword);
        result.Succeeded.ShouldBeTrue(string.Join("; ", result.Errors.Select(error => error.Description)));

        return user;
    }

    private async Task WithCurrentUserAsync(IdentityUser user, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, user.Id.ToString()),
                    new Claim(AbpClaimTypes.UserName, user.UserName),
                    new Claim(AbpClaimTypes.Email, user.Email)
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }

    private static void AssertIndex(
        System.Collections.Generic.IReadOnlyDictionary<string, Microsoft.EntityFrameworkCore.Metadata.IIndex> indexes,
        string name,
        string[] properties,
        string filter)
    {
        indexes.ContainsKey(name).ShouldBeTrue();
        var index = indexes[name];
        index.IsUnique.ShouldBeTrue();
        index.Properties.Select(property => property.Name).ShouldBe(properties);
        index.GetFilter().ShouldBe(filter);
    }
}
