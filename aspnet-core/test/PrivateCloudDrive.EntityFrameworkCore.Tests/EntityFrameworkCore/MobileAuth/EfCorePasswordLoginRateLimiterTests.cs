using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示移动认证EfCorePasswordLoginRateLimiterTests，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCorePasswordLoginRateLimiterTests : PrivateCloudDrive.EntityFrameworkCore.PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IPasswordLoginRateLimiter _rateLimiter;
    private readonly MobileAuthLoginOptions _options;

    /// <summary>
    /// 初始化 <see cref="EfCorePasswordLoginRateLimiterTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCorePasswordLoginRateLimiterTests()
    {
        _rateLimiter = GetRequiredService<IPasswordLoginRateLimiter>();
        _options = GetRequiredService<IOptions<MobileAuthLoginOptions>>().Value;
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Rate_Limit_Password_Login_By_UserName()
    {
        const string userName = "rate-user@example.test";

        for (var attempt = 0; attempt < _options.MaxFailedAttempts; attempt++)
        {
            await _rateLimiter.RecordFailureAsync(userName, $"10.10.0.{attempt}");
        }

        var rateLimited = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _rateLimiter.CheckAsync(userName, "10.10.0.250");
        });

        rateLimited.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.PasswordLoginRateLimited);
        rateLimited.Data["error"].ShouldBe(PasswordLoginConsts.RateLimitedError);

        await _rateLimiter.CheckAsync("other-rate-user@example.test", "10.10.0.250");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Rate_Limit_Password_Login_By_Ip()
    {
        const string ipAddress = "10.20.0.10";

        for (var attempt = 0; attempt < _options.MaxFailedAttempts; attempt++)
        {
            await _rateLimiter.RecordFailureAsync($"ip-user-{attempt}@example.test", ipAddress);
        }

        var rateLimited = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _rateLimiter.CheckAsync("new-ip-user@example.test", ipAddress);
        });

        rateLimited.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.PasswordLoginRateLimited);
        rateLimited.Data["error"].ShouldBe(PasswordLoginConsts.RateLimitedError);

        await _rateLimiter.CheckAsync("new-ip-user@example.test", "10.20.0.11");
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reset_User_Rate_Limit_After_Successful_Login()
    {
        const string userName = "reset-user@example.test";

        for (var attempt = 0; attempt < _options.MaxFailedAttempts; attempt++)
        {
            await _rateLimiter.RecordFailureAsync(userName, $"10.30.0.{attempt}");
        }

        await _rateLimiter.ResetUserAsync(userName);
        await _rateLimiter.CheckAsync(userName, "10.30.0.250");
    }
}
