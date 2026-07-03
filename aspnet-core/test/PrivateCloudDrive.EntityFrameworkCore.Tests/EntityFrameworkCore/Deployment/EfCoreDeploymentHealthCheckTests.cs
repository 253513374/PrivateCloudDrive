using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PrivateCloudDrive.Deployment;
using PrivateCloudDrive.FileCenter;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.Deployment;

/// <summary>
/// 验证部署健康检查端点，确保运维人员部署后可通过单条 API 调用确认系统就绪。
/// 所有输出不包含密码、token、OAuth code、client secret 或完整私有 URL。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreDeploymentHealthCheckTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IDeploymentHealthCheckService _deploymentHealthCheckService;
    private readonly IConfiguration _configuration;
    private readonly FileCenterMediaProcessingOptions _mediaProcessingOptions;

    /// <summary>
    /// 初始化 <see cref="EfCoreDeploymentHealthCheckTests"/> 的新实例。
    /// </summary>
    public EfCoreDeploymentHealthCheckTests()
    {
        _deploymentHealthCheckService = GetRequiredService<IDeploymentHealthCheckService>();
        _configuration = GetRequiredService<IConfiguration>();
        _mediaProcessingOptions = GetRequiredService<IOptions<FileCenterMediaProcessingOptions>>().Value;
    }

    /// <summary>
    /// 验证部署健康检查返回所有 8 个检查项，且整体状态与分项一致。
    /// </summary>
    [Fact]
    public async Task Should_Return_All_Deployment_Health_Checks()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        // 验证所有 8 个检查项都存在
        result.Checks.ShouldNotBeNull();
        result.Checks.Count.ShouldBeGreaterThanOrEqualTo(8);

        // 验证检查项名称完整性
        var checkNames = result.Checks.Select(c => c.Name).ToList();
        checkNames.ShouldContain("数据库连接");
        checkNames.ShouldContain("Redis/分布式缓存");
        checkNames.ShouldContain("存储卷可写性");
        checkNames.ShouldContain("FFmpeg");
        checkNames.ShouldContain("FFprobe");
        checkNames.ShouldContain("OpenIddict Issuer URL");
        checkNames.ShouldContain("OpenIddict 应用 Redirect URLs");
        checkNames.ShouldContain("安全配置检查");

        // 验证每个检查项都有 Name、Status、Message
        foreach (var check in result.Checks)
        {
            check.Name.ShouldNotBeNullOrWhiteSpace();
            check.Message.ShouldNotBeNullOrWhiteSpace();
        }

        // 验证 OverallStatus 与分项一致
        // Fail 任一 → 整体 Fail
        if (result.Checks.Any(c => c.Status == DeploymentCheckStatus.Fail))
        {
            result.OverallStatus.ShouldBe(DeploymentCheckStatus.Fail);
        }
        else if (result.Checks.Any(c => c.Status == DeploymentCheckStatus.Warn))
        {
            result.OverallStatus.ShouldBe(DeploymentCheckStatus.Warn);
        }
        else
        {
            result.OverallStatus.ShouldBe(DeploymentCheckStatus.Pass);
        }

        // 验证时间戳
        result.GeneratedAt.ShouldBeGreaterThan(DateTime.MinValue);
        result.GeneratedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 验证部署健康检查输出不包含任何敏感信息：密码、token、OAuth code、client secret、完整私有 URL。
    /// </summary>
    [Fact]
    public async Task Should_Not_Expose_Sensitive_Data()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var allOutputText = string.Join(
            "\n",
            result.Checks.Select(c =>
                $"{c.Name}|{c.Status}|{c.Message}|{c.FixSuggestion}"));

        // 禁止的敏感标记
        var forbiddenMarkers = new[]
        {
            "Password=",
            "myPassword",
            "privateclouddrive",
            "client_secret",
            "client secret",
            "AccessKeyId",
            "AccessKeySecret",
            "NWdpATI5trUHk4X2",
            "raw exception"
        };

        foreach (var marker in forbiddenMarkers)
        {
            allOutputText.ShouldNotContain(marker, Case.Insensitive);
        }

        // Verify the flag
        result.ContainsSensitiveData.ShouldBeFalse();
    }

    /// <summary>
    /// 验证部署健康检查无需用户认证即可访问（AllowAnonymous）。
    /// 不设置当前用户身份即可成功调用。
    /// </summary>
    [Fact]
    public async Task Should_Be_Accessible_Without_Authentication()
    {
        // 直接调用服务（不设置任何用户身份），验证不抛出认证异常
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        result.ShouldNotBeNull();
        result.Checks.Count.ShouldBeGreaterThanOrEqualTo(8);
    }

    /// <summary>
    /// 验证数据库连接检查在测试环境下因缺少 Npgsql 提供程序而返回 Warn 而非 Fail。
    /// 测试环境使用 SQLite 内存数据库，Npgsql 未注册，检查会降级为 Warn。
    /// </summary>
    [Fact]
    public async Task Should_Return_Warn_When_Npgsql_Unavailable()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var dbCheck = result.Checks.Single(c => c.Name == "数据库连接");
        dbCheck.Status.ShouldBe(DeploymentCheckStatus.Warn);
        dbCheck.Message.ShouldContain("Npgsql/PostgreSQL");
    }

    /// <summary>
    /// 验证安全配置检查在测试环境下返回 Warn（包含已知默认值警告）。
    /// </summary>
    [Fact]
    public async Task Should_Return_Warn_For_Default_Security_Settings()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var securityCheck = result.Checks.Single(c => c.Name == "安全配置检查");
        securityCheck.Status.ShouldBe(DeploymentCheckStatus.Warn);
        securityCheck.FixSuggestion.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 验证检查项不暴露完整存储路径，仅显示安全摘要。
    /// </summary>
    [Fact]
    public async Task Should_Not_Expose_Full_Storage_Path()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var storageCheck = result.Checks.Single(c => c.Name == "存储卷可写性");
        // 验证输出没有完整绝对路径（如 D:\Devs\）
        storageCheck.Message.ShouldNotContain("D:\\");
        storageCheck.Message.ShouldNotContain("D:/");
    }

    /// <summary>
    /// 验证 OpenIddict Issuer URL 检查在测试配置下返回正确状态。
    /// </summary>
    [Fact]
    public async Task Should_Check_OpenIddict_Issuer_Url()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var issuerCheck = result.Checks.Single(c => c.Name == "OpenIddict Issuer URL");
        issuerCheck.Message.ShouldNotBeNullOrWhiteSpace();
        // 应反映配置中的 AuthServer:Authority
        var authority = _configuration["AuthServer:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            issuerCheck.Message.ShouldContain(authority.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "HTTPS" : "http");
        }
    }

    /// <summary>
    /// 验证 OpenIddict 应用 Redirect URLs 检查返回结果。
    /// </summary>
    [Fact]
    public async Task Should_Check_OpenIddict_Redirect_Urls()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var redirectCheck = result.Checks.Single(c => c.Name == "OpenIddict 应用 Redirect URLs");
        redirectCheck.Message.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 验证 FFmpeg/FFprobe 检查存在且结果类型正确。
    /// </summary>
    [Fact]
    public async Task Should_Check_Media_Tools()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        // FFmpeg
        var ffmpegCheck = result.Checks.Single(c => c.Name == "FFmpeg");
        ffmpegCheck.Message.ShouldNotBeNullOrWhiteSpace();
        ffmpegCheck.Status.ShouldBeOneOf(DeploymentCheckStatus.Pass, DeploymentCheckStatus.Warn, DeploymentCheckStatus.Fail);

        // FFprobe
        var ffprobeCheck = result.Checks.Single(c => c.Name == "FFprobe");
        ffprobeCheck.Message.ShouldNotBeNullOrWhiteSpace();
        ffprobeCheck.Status.ShouldBeOneOf(DeploymentCheckStatus.Pass, DeploymentCheckStatus.Warn, DeploymentCheckStatus.Fail);
    }

    /// <summary>
    /// 验证 Swagger 生产环境检查逻辑存在且不干扰其他检查项。
    /// 测试配置中 Swagger:Enabled 为 false，验证检查未误报。
    /// </summary>
    [Fact]
    public async Task Should_Not_Fail_When_Swagger_Disabled_In_Test()
    {
        var result = await _deploymentHealthCheckService.GetHealthAsync();

        var securityCheck = result.Checks.Single(c => c.Name == "安全配置检查");
        securityCheck.ShouldNotBeNull();

        // Swagger 检查失败语不应出现在测试环境的安全检查结果中
        // （测试配置中 Swagger:Enabled=false，且 ASPNETCORE_ENVIRONMENT 不为 Development）
        if (securityCheck.FixSuggestion != null)
        {
            securityCheck.FixSuggestion.ShouldNotContain("Swagger");
        }
    }
}
