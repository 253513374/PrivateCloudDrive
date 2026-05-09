using System;
using System.Linq;
using System.Threading.Tasks;
using PrivateCloudDrive.MobileAuth;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.OperationLogs;

/// <summary>
/// 表示EfCoreOperationLogsAppServiceTests组件，封装对应业务场景的状态或行为。
/// </summary>
public class EfCoreOperationLogsAppServiceTests : PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IMobileAuthAuditLogsAppService _mobileAuthAuditLogsAppService;
    private readonly IOperationLogsAppService _operationLogsAppService;

    /// <summary>
    /// 初始化 <see cref="EfCoreOperationLogsAppServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreOperationLogsAppServiceTests()
    {
        _mobileAuthAuditLogsAppService = GetRequiredService<IMobileAuthAuditLogsAppService>();
        _operationLogsAppService = GetRequiredService<IOperationLogsAppService>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Query_Mobile_Auth_Operation_Logs_With_Filters()
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
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Filter_Operation_Logs_By_Time_Range()
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
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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
}
