using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrivateCloudDrive.EntityFrameworkCore;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证EfCoreWechatAuthAppServiceTests，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreWechatAuthAppServiceTests : PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private const string TestPassword = "P@ssw0rd1";

    private readonly IWechatLoginService _wechatLoginService;
    private readonly IWechatAuthAppService _wechatAuthAppService;
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<WechatUserBinding, Guid> _bindingRepository;
    private readonly IRepository<MobileAuthAuditLog, Guid> _auditLogRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly WechatLoginOptions _wechatOptions;

    /// <summary>
    /// 初始化 <see cref="EfCoreWechatAuthAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreWechatAuthAppServiceTests()
    {
        _wechatLoginService = GetRequiredService<IWechatLoginService>();
        _wechatAuthAppService = GetRequiredService<IWechatAuthAppService>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _bindingRepository = GetRequiredService<IRepository<WechatUserBinding, Guid>>();
        _auditLogRepository = GetRequiredService<IRepository<MobileAuthAuditLog, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _wechatOptions = GetRequiredService<IOptions<WechatLoginOptions>>().Value;
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Create_Binding_Ticket_When_Wechat_Login_Is_Not_Bound()
    {
        var result = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.AliceCode,
            ClientId = "PrivateCloudDrive_App",
            DeviceIdHash = "device-hash-1"
        });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(WechatLoginConsts.BindingRequiredError);
        result.BindingTicket.ShouldNotBeNullOrWhiteSpace();

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatLogin &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.BindingRequiredError &&
            log.ClientId == "PrivateCloudDrive_App" &&
            log.DeviceIdHash == "device-hash-1");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Bind_Existing_User_And_Login_With_Wechat()
    {
        var user = await CreateUserAsync("wechat-user", "wechat-user@example.test");
        var firstLogin = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.AliceCode,
            ClientId = "PrivateCloudDrive_App"
        });

        firstLogin.BindingTicket.ShouldNotBeNullOrWhiteSpace();

        var bindingDto = await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
        {
            BindingTicket = firstLogin.BindingTicket!,
            UserNameOrEmail = user.UserName,
            Password = TestPassword
        });

        bindingDto.UserId.ShouldBe(user.Id);
        bindingDto.AppId.ShouldBe(TestWechatIdentityService.AppId);
        bindingDto.NickName.ShouldBe("Alice");

        var secondLogin = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.AliceUpdatedCode,
            ClientId = "PrivateCloudDrive_App"
        });

        secondLogin.Succeeded.ShouldBeTrue();
        secondLogin.UserId.ShouldBe(user.Id);
        secondLogin.UserName.ShouldBe(user.UserName);

        var binding = (await _bindingRepository.GetListAsync())
            .Single(item => item.UserId == user.Id);
        binding.NickName.ShouldBe("Alice Updated");
        binding.IsEnabled.ShouldBeTrue();
        binding.LastLoginTime.ShouldNotBeNull();

        var consumedTicket = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
            {
                BindingTicket = firstLogin.BindingTicket!,
                UserNameOrEmail = user.UserName,
                Password = TestPassword
            });
        });

        consumedTicket.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.WeChatBindingTicketNotFound);

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatBind &&
            log.Result == MobileAuthAuditLogResults.Success &&
            log.UserId == user.Id);
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatLogin &&
            log.Result == MobileAuthAuditLogResults.Success &&
            log.UserId == user.Id);
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Prevent_Wechat_Binding_From_Moving_To_Another_User()
    {
        var owner = await CreateUserAsync("wechat-owner", "wechat-owner@example.test");
        var otherUser = await CreateUserAsync("wechat-other", "wechat-other@example.test");
        var firstLogin = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.AliceCode
        });

        await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
        {
            BindingTicket = firstLogin.BindingTicket!,
            UserNameOrEmail = owner.UserName,
            Password = TestPassword
        });

        await WithCurrentUserAsync(otherUser, async () =>
        {
            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _wechatAuthAppService.BindCurrentAsync(new BindCurrentWechatInput
                {
                    Code = TestWechatIdentityService.AliceCode,
                    DeviceIdHash = "device-hash-2"
                });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.WeChatAlreadyBound);
        });

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatBind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.AlreadyBoundError &&
            log.UserId == otherUser.Id &&
            log.DeviceIdHash == "device-hash-2");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Count_Failed_Bind_Existing_Password_Attempts_Without_Consuming_Ticket()
    {
        var user = await CreateUserAsync("wechat-bind-failed-password", "wechat-bind-failed-password@example.test");
        var lockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
        lockoutResult.Succeeded.ShouldBeTrue(string.Join("; ", lockoutResult.Errors.Select(error => error.Description)));

        var firstLogin = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.BobCode
        });

        firstLogin.BindingTicket.ShouldNotBeNullOrWhiteSpace();

        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
            {
                BindingTicket = firstLogin.BindingTicket!,
                UserNameOrEmail = user.UserName,
                Password = "wrong-password"
            });
        });

        var afterFailure = await _userManager.FindByIdAsync(user.Id.ToString());
        afterFailure.ShouldNotBeNull();
        afterFailure!.AccessFailedCount.ShouldBe(1);

        var binding = await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
        {
            BindingTicket = firstLogin.BindingTicket!,
            UserNameOrEmail = user.UserName,
            Password = TestPassword
        });

        binding.UserId.ShouldBe(user.Id);

        var afterSuccess = await _userManager.FindByIdAsync(user.Id.ToString());
        afterSuccess.ShouldNotBeNull();
        afterSuccess!.AccessFailedCount.ShouldBe(0);

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatBind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == "invalid_user_credentials" &&
            log.UserId == user.Id);
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatBind &&
            log.Result == MobileAuthAuditLogResults.Success &&
            log.UserId == user.Id);
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Prevent_Locked_Out_User_From_Login_With_Bound_Wechat()
    {
        var user = await CreateUserAsync("wechat-locked-login", "wechat-locked-login@example.test");
        var firstLogin = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.AliceCode
        });

        await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
        {
            BindingTicket = firstLogin.BindingTicket!,
            UserNameOrEmail = user.UserName,
            Password = TestPassword
        });

        var enableLockout = await _userManager.SetLockoutEnabledAsync(user, true);
        enableLockout.Succeeded.ShouldBeTrue(string.Join("; ", enableLockout.Errors.Select(error => error.Description)));
        var setLockout = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        setLockout.Succeeded.ShouldBeTrue(string.Join("; ", setLockout.Errors.Select(error => error.Description)));

        var result = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.AliceUpdatedCode,
            ClientId = "PrivateCloudDrive_App",
            DeviceIdHash = "locked-device"
        });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("invalid_grant");
        result.BindingTicket.ShouldBeNull();

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatLogin &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == "user_locked_out" &&
            log.UserId == user.Id &&
            log.ClientId == "PrivateCloudDrive_App" &&
            log.DeviceIdHash == "locked-device");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Unbind_Current_User_Without_Removing_Password_Login()
    {
        var user = await CreateUserAsync("wechat-unbind", "wechat-unbind@example.test");
        var firstLogin = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.BobCode
        });

        await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
        {
            BindingTicket = firstLogin.BindingTicket!,
            UserNameOrEmail = user.UserName,
            Password = TestPassword
        });

        await WithCurrentUserAsync(user, async () =>
        {
            var currentBinding = await _wechatAuthAppService.GetBindingAsync();
            currentBinding.ShouldNotBeNull();

            await _wechatAuthAppService.UnbindAsync();

            var afterUnbind = await _wechatAuthAppService.GetBindingAsync();
            afterUnbind.ShouldBeNull();
        });

        var binding = (await _bindingRepository.GetListAsync())
            .Single(item => item.UserId == user.Id);
        binding.IsEnabled.ShouldBeFalse();

        var storedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        storedUser.ShouldNotBeNull();
        storedUser!.PasswordHash.ShouldNotBeNullOrWhiteSpace();

        var loginAfterUnbind = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.BobCode
        });

        loginAfterUnbind.Succeeded.ShouldBeFalse();
        loginAfterUnbind.Error.ShouldBe(WechatLoginConsts.BindingRequiredError);
        loginAfterUnbind.BindingTicket.ShouldNotBeNullOrWhiteSpace();

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatUnbind &&
            log.Result == MobileAuthAuditLogResults.Success &&
            log.UserId == user.Id);
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Audit_Unbind_When_Current_User_Has_No_Wechat_Binding()
    {
        var user = await CreateUserAsync("wechat-unbind-none", "wechat-unbind-none@example.test");

        await WithCurrentUserAsync(user, async () =>
        {
            await _wechatAuthAppService.UnbindAsync();
        });

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatUnbind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.BindingNotFoundError &&
            log.UserId == user.Id);
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Record_Safe_Wechat_Exchange_Failure_Without_Secrets()
    {
        var result = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = TestWechatIdentityService.FailedCode,
            ClientId = "PrivateCloudDrive_App"
        });

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(WechatLoginConsts.CodeExchangeFailedError);

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatLogin &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.CodeExchangeFailedError);
        auditLogs.ShouldNotContain(log =>
            log.FailureReason != null &&
            log.FailureReason.Contains(TestWechatIdentityService.FailedCode, StringComparison.Ordinal));
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Rate_Limit_Wechat_Login_Attempts()
    {
        for (var attempt = 0; attempt < _wechatOptions.RateLimitMaxAttempts; attempt++)
        {
            var result = await _wechatLoginService.LoginAsync(new WechatLoginInput
            {
                Code = $"rate-login-{attempt}",
                ClientId = "RateLimitClient",
                DeviceIdHash = "rate-login-device",
                Platform = "android"
            });

            result.Succeeded.ShouldBeFalse();
            result.Error.ShouldBe(WechatLoginConsts.BindingRequiredError);
        }

        var rateLimited = await _wechatLoginService.LoginAsync(new WechatLoginInput
        {
            Code = "rate-login-limited",
            ClientId = "RateLimitClient",
            DeviceIdHash = "rate-login-device",
            Platform = "android"
        });

        rateLimited.Succeeded.ShouldBeFalse();
        rateLimited.Error.ShouldBe(WechatLoginConsts.RateLimitedError);
        rateLimited.BindingTicket.ShouldBeNull();

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatLogin &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.RateLimitedError &&
            log.ClientId == "RateLimitClient" &&
            log.DeviceIdHash == "rate-login-device");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Rate_Limit_Wechat_Bind_Existing_Attempts()
    {
        for (var attempt = 0; attempt < _wechatOptions.RateLimitMaxAttempts; attempt++)
        {
            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
                {
                    BindingTicket = $"missing-rate-ticket-{attempt}",
                    UserNameOrEmail = "rate-limit-bind@example.test",
                    Password = TestPassword
                });
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.WeChatBindingTicketNotFound);
        }

        var rateLimited = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _wechatAuthAppService.BindExistingAsync(new BindExistingWechatInput
            {
                BindingTicket = "missing-rate-ticket-limited",
                UserNameOrEmail = "rate-limit-bind@example.test",
                Password = TestPassword
            });
        });

        rateLimited.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.WeChatRateLimited);
        rateLimited.Data["error"].ShouldBe(WechatLoginConsts.RateLimitedError);

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatBind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.RateLimitedError &&
            log.UserName == "rate-limit-bind@example.test");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Rate_Limit_Wechat_Bind_Current_And_Unbind_Attempts()
    {
        var user = await CreateUserAsync("wechat-rate-current", "wechat-rate-current@example.test");

        await WithCurrentUserAsync(user, async () =>
        {
            for (var attempt = 0; attempt < _wechatOptions.RateLimitMaxAttempts; attempt++)
            {
                var binding = await _wechatAuthAppService.BindCurrentAsync(new BindCurrentWechatInput
                {
                    Code = TestWechatIdentityService.AliceCode,
                    DeviceIdHash = "rate-current-device"
                });

                binding.UserId.ShouldBe(user.Id);
            }

            var bindRateLimited = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _wechatAuthAppService.BindCurrentAsync(new BindCurrentWechatInput
                {
                    Code = TestWechatIdentityService.AliceUpdatedCode,
                    DeviceIdHash = "rate-current-device"
                });
            });

            bindRateLimited.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.WeChatRateLimited);

            for (var attempt = 0; attempt < _wechatOptions.RateLimitMaxAttempts; attempt++)
            {
                await _wechatAuthAppService.UnbindAsync();
            }

            var unbindRateLimited = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _wechatAuthAppService.UnbindAsync();
            });

            unbindRateLimited.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.WeChatRateLimited);
        });

        var auditLogs = await _auditLogRepository.GetListAsync();
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatBind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.RateLimitedError &&
            log.UserId == user.Id &&
            log.DeviceIdHash == "rate-current-device");
        auditLogs.ShouldContain(log =>
            log.Provider == MobileAuthAuditLogProviders.WeChat &&
            log.Action == MobileAuthAuditLogActions.WeChatUnbind &&
            log.Result == MobileAuthAuditLogResults.Failed &&
            log.FailureReason == WechatLoginConsts.RateLimitedError &&
            log.UserId == user.Id);
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public void Should_Keep_Wechat_Output_Dtos_Free_From_Secrets()
    {
        var propertyNames = typeof(WechatLoginSettingsDto)
            .GetProperties()
            .Concat(typeof(WechatBindingDto).GetProperties())
            .Select(property => property.Name)
            .ToList();

        propertyNames.ShouldNotContain("AppSecret");
        propertyNames.ShouldNotContain("OpenId");
        propertyNames.ShouldNotContain("UnionId");
        propertyNames.ShouldNotContain("AccessToken");
        propertyNames.ShouldNotContain("RefreshToken");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public void Should_Use_Postgres_Safe_Unique_Indexes_For_Wechat_Bindings()
    {
        var dbContext = GetRequiredService<PrivateCloudDriveDbContext>();
        var entityType = dbContext.Model.FindEntityType(typeof(WechatUserBinding));
        entityType.ShouldNotBeNull();

        var indexes = entityType!.GetIndexes()
            .ToDictionary(index => index.GetDatabaseName()!);

        indexes.ContainsKey("UX_WechatUserBindings_AppId_OpenId").ShouldBeFalse();
        indexes.ContainsKey("UX_WechatUserBindings_UnionId").ShouldBeFalse();

        AssertIndex(
            indexes,
            "UX_WechatUserBindings_Host_AppId_OpenId",
            new[] { nameof(WechatUserBinding.AppId), nameof(WechatUserBinding.OpenId) },
            "\"TenantId\" IS NULL");

        AssertIndex(
            indexes,
            "UX_WechatUserBindings_Tenant_AppId_OpenId",
            new[] { nameof(WechatUserBinding.TenantId), nameof(WechatUserBinding.AppId), nameof(WechatUserBinding.OpenId) },
            "\"TenantId\" IS NOT NULL");

        AssertIndex(
            indexes,
            "UX_WechatUserBindings_Host_UnionId",
            new[] { nameof(WechatUserBinding.UnionId) },
            "\"TenantId\" IS NULL AND \"UnionId\" IS NOT NULL");

        AssertIndex(
            indexes,
            "UX_WechatUserBindings_Tenant_UnionId",
            new[] { nameof(WechatUserBinding.TenantId), nameof(WechatUserBinding.UnionId) },
            "\"TenantId\" IS NOT NULL AND \"UnionId\" IS NOT NULL");
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
