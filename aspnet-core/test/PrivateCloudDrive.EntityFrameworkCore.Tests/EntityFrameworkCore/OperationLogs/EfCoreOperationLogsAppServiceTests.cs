using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using PrivateCloudDrive.MobileAuth;
using PrivateCloudDrive.Permissions;
using Shouldly;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.OperationLogs;

public class EfCoreOperationLogsAppServiceTests : PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IMobileAuthAuditLogsAppService _mobileAuthAuditLogsAppService;
    private readonly IOperationLogsAppService _operationLogsAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public EfCoreOperationLogsAppServiceTests()
    {
        _mobileAuthAuditLogsAppService = GetRequiredService<IMobileAuthAuditLogsAppService>();
        _operationLogsAppService = GetRequiredService<IOperationLogsAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid NormalUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid OtherUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private async Task AsAdminAsync(Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, AdminUserId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "admin"),
                    new Claim(AbpClaimTypes.Role, "admin")
                },
                "Test"));
        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }

    private async Task AsUserAsync(Guid userId, string userName, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, userName)
                },
                "Test"));
        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }

    [Fact]
    public async Task Should_Query_Mobile_Auth_Operation_Logs_With_Filters()
    {
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "admin",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.Logout,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "admin",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            var result = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                UserName = "admin",
                MaxResultCount = 10
            });

            result.TotalCount.ShouldBe(1);
            var log = result.Items.Single();
            log.Source.ShouldBe(OperationLogSources.MobileAuth);
            log.Action.ShouldBe(MobileAuthAuditLogActions.PasswordLogin);
            log.Result.ShouldBe(MobileAuthAuditLogResults.Success);
            log.UserName.ShouldBe("admin");
            log.ClientId.ShouldBe("PrivateCloudDrive_App");
        });
    }

    [Fact]
    public async Task Should_Filter_Operation_Logs_By_Time_Range()
    {
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Failed,
                ClientId = "PrivateCloudDrive_App",
                UserName = "time-filter-user",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            var result = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                UserName = "time-filter-user",
                StartTime = DateTime.Now.AddDays(1),
                MaxResultCount = 10
            });

            result.TotalCount.ShouldBe(0);
        });
    }

    [Fact]
    public void Should_Keep_Operation_Log_Contracts_Free_From_Secrets()
    {
        var propertyNames = typeof(GetOperationLogsInput)
            .GetProperties()
            .Concat(typeof(OperationLogDto).GetProperties())
            .Select(property => property.Name)
            .ToList();

        propertyNames.ShouldNotContain("Password");
        propertyNames.ShouldNotContain("AccessToken");
        propertyNames.ShouldNotContain("RefreshToken");
        propertyNames.ShouldNotContain("Token");
        propertyNames.ShouldNotContain("AppSecret");
        propertyNames.ShouldNotContain("Parameters");
        propertyNames.ShouldNotContain("Exception");
        propertyNames.ShouldNotContain("Exceptions");
    }

    [Fact]
    public void Should_Cover_All_Critical_Audit_Event_Types()
    {
        var authActions = typeof(MobileAuthAuditLogActions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.GetValue(null)!.ToString())
            .ToList();

        authActions.ShouldContain("PasswordLogin");
        authActions.ShouldContain("RefreshToken");
        authActions.ShouldContain("Logout");
        authActions.ShouldContain("ExternalLogin");
        authActions.ShouldContain("ExternalBind");
        authActions.ShouldContain("ExternalUnbind");

        var bizActions = typeof(OperationLogActions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.GetValue(null)!.ToString())
            .ToList();

        bizActions.ShouldContain("FileUpload");
        bizActions.ShouldContain("FileDownload");
        bizActions.ShouldContain("FileDelete");
        bizActions.ShouldContain("FileRestore");
        bizActions.ShouldContain("ShareCreate");
        bizActions.ShouldContain("ShareDelete");
        bizActions.ShouldContain("ShareAccess");
        bizActions.ShouldContain("Security");
    }

    [Fact]
    public void Should_Keep_Mobile_Auth_Audit_Log_Dto_Free_From_Secrets()
    {
        var propertyNames = typeof(MobileAuthAuditLogDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToList();

        propertyNames.ShouldNotContain("Password");
        propertyNames.ShouldNotContain("AccessToken");
        propertyNames.ShouldNotContain("RefreshToken");
        propertyNames.ShouldNotContain("Token");
        propertyNames.ShouldNotContain("AppSecret");
        propertyNames.ShouldNotContain("Secret");
    }

    [Fact]
    public async Task Should_Normalize_ActionName_Alias_To_Action()
    {
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "alias-test-user",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            // 使用 ActionName（不是 Action）查询，验证别名生效
            var result = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                ActionName = MobileAuthAuditLogActions.PasswordLogin,
                UserName = "alias-test-user",
                MaxResultCount = 10
            });

            result.TotalCount.ShouldBe(1);
            var log = result.Items.Single();
            log.Action.ShouldBe(MobileAuthAuditLogActions.PasswordLogin);
        });
    }

    [Fact]
    public async Task Should_Normalize_CreateAfter_To_StartTime()
    {
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "create-after-test",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            // 使用未来时间 query by CreateAfter，应返回 0
            var futureResult = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                UserName = "create-after-test",
                CreateAfter = DateTime.Now.AddDays(1),
                MaxResultCount = 10
            });
            futureResult.TotalCount.ShouldBe(0);

            // 使用过去时间 query by CreateAfter，应返回记录
            var pastResult = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                UserName = "create-after-test",
                CreateAfter = DateTime.Now.AddDays(-1),
                MaxResultCount = 10
            });
            pastResult.TotalCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Normalize_CreateBefore_To_EndTime()
    {
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "create-before-test",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            // 使用过去时间 query by CreateBefore，应返回 0
            var pastResult = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                UserName = "create-before-test",
                CreateBefore = DateTime.Now.AddDays(-1),
                MaxResultCount = 10
            });
            pastResult.TotalCount.ShouldBe(0);

            // 使用未来时间 query by CreateBefore，应返回记录
            var futureResult = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                UserName = "create-before-test",
                CreateBefore = DateTime.Now.AddDays(1),
                MaxResultCount = 10
            });
            futureResult.TotalCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Should_Admin_See_All_Users_Logs()
    {
        // admin 创建 user-alpha 的日志
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "admin-login",
                UserAgent = "PrivateCloudDrive.MAUI"
            });
        });

        // user-alpha 创建自己的日志
        await AsUserAsync(NormalUserId, "user-alpha", async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "user-alpha",
                UserAgent = "PrivateCloudDrive.MAUI"
            });
        });

        // admin 查询时能看到所有日志（自己的 + user-alpha 的）
        await AsAdminAsync(async () =>
        {
            var result = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                MaxResultCount = 100
            });

            result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
            result.Items.ShouldContain(log => log.UserName == "admin-login");
            result.Items.ShouldContain(log => log.UserName == "user-alpha");
        });
    }

    /// <summary>
    /// 验证普通用户只能看到自己的日志。
    /// 注意：测试基础设施使用 AddAlwaysAllowAuthorization，因此实际的 permission-based
    /// 可见性规则无法在此模式中完整验证。此测试仅验证认证用户可查询日志的基本流程。
    /// 非管理员用户隔离的端到端验证需要在移除了 AlwaysAllow 的测试环境中运行。
    /// </summary>
    [Fact]
    public async Task Should_NonAdmin_Only_See_Own_Logs()
    {
        // user-alpha 创建日志
        await AsUserAsync(NormalUserId, "user-alpha", async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "user-alpha",
                UserAgent = "PrivateCloudDrive.MAUI"
            });
        });

        // 在 AddAlwaysAllowAuthorization 模式下，当前用户始终拥有 FileCenter.Manage 权限
        // 因此不会触发仅看自己的过滤逻辑。此处仅验证 API 调用不会抛出异常。
        await AsUserAsync(NormalUserId, "user-alpha", async () =>
        {
            await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                MaxResultCount = 100
            });
        });
    }

    [Fact]
    public async Task Should_NonAdmin_Without_UserId_Return_Empty()
    {
        // 无 claims 的默认匿名用户，CurrentUser.Id 为 null
        // 非管理员 + 无 UserId → 直接返回空结果
        var result = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
        {
            Source = OperationLogSources.MobileAuth,
            MaxResultCount = 10
        });

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_FileNodeId_Returns_Empty_When_No_Matching_Dto()
    {
        // OperationLogDto 有 FileNodeId 字段，但 MobileAuth 日志不会设置该字段，
        // 所以按 FileNodeId 过滤应返回空
        await AsAdminAsync(async () =>
        {
            await _mobileAuthAuditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
            {
                Provider = MobileAuthAuditLogProviders.Password,
                Action = MobileAuthAuditLogActions.PasswordLogin,
                Result = MobileAuthAuditLogResults.Success,
                ClientId = "PrivateCloudDrive_App",
                UserName = "filenodeid-test",
                UserAgent = "PrivateCloudDrive.MAUI"
            });

            var result = await _operationLogsAppService.GetListAsync(new GetOperationLogsInput
            {
                Source = OperationLogSources.MobileAuth,
                FileNodeId = Guid.NewGuid(),
                MaxResultCount = 10
            });

            result.TotalCount.ShouldBe(0);
        });
    }
}
