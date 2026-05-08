using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Xunit;

namespace PrivateCloudDrive.MobileAuth;

public class EfCoreMobileAuthAuditLogsAppServiceTests : PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IMobileAuthAuditLogsAppService _auditLogsAppService;

    public EfCoreMobileAuthAuditLogsAppServiceTests()
    {
        _auditLogsAppService = GetRequiredService<IMobileAuthAuditLogsAppService>();
    }

    [Fact]
    public async Task Should_Record_And_Query_Mobile_Auth_Audit_Log()
    {
        await _auditLogsAppService.RecordAsync(new CreateMobileAuthAuditLogInput
        {
            Provider = MobileAuthAuditLogProviders.Password,
            Action = MobileAuthAuditLogActions.PasswordLogin,
            Result = MobileAuthAuditLogResults.Failed,
            FailureReason = "Invalid username or password.",
            ClientId = "PrivateCloudDrive_App",
            UserName = "admin",
            UserAgent = "PrivateCloudDrive.MAUI"
        });

        var result = await _auditLogsAppService.GetListAsync(new PagedResultRequestDto
        {
            MaxResultCount = 10
        });

        var auditLog = result.Items.FirstOrDefault(item =>
            item.Provider == MobileAuthAuditLogProviders.Password &&
            item.Action == MobileAuthAuditLogActions.PasswordLogin &&
            item.Result == MobileAuthAuditLogResults.Failed);

        auditLog.ShouldNotBeNull();
        auditLog!.FailureReason.ShouldBe("Invalid username or password.");
        auditLog.ClientId.ShouldBe("PrivateCloudDrive_App");
        auditLog.UserName.ShouldBe("admin");
        auditLog.UserAgent.ShouldBe("PrivateCloudDrive.MAUI");
    }

    [Fact]
    public void Should_Keep_Audit_Input_And_Dto_Free_From_Secrets()
    {
        var propertyNames = typeof(CreateMobileAuthAuditLogInput)
            .GetProperties()
            .Concat(typeof(MobileAuthAuditLogDto).GetProperties())
            .Select(property => property.Name)
            .ToList();

        propertyNames.ShouldNotContain("Password");
        propertyNames.ShouldNotContain("AccessToken");
        propertyNames.ShouldNotContain("RefreshToken");
        propertyNames.ShouldNotContain("Token");
    }
}
