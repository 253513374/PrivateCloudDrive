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
/// 覆盖 live/ready/detail 分层与脱敏。
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
    /// 覆盖增强脱敏（Windows 路径、Linux 路径、连接串密码、JWT token、AccessKeySecret）。
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
        if (securityCheck.FixSuggestion != null)
        {
            securityCheck.FixSuggestion.ShouldNotContain("Swagger");
        }
    }

    // ============================
    // 分层检查：live
    // ============================

    /// <summary>
    /// 验证 /health/live 返回极简存活状态，不依赖任何外部服务。
    /// </summary>
    [Fact]
    public async Task Should_Return_Live_Status()
    {
        var result = await _deploymentHealthCheckService.GetLiveAsync();

        result.ShouldNotBeNull();
        result.Status.ShouldBe("Healthy");
        result.GeneratedAt.ShouldBeGreaterThan(DateTime.MinValue);
        result.GeneratedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    // ============================
    // 分层检查：ready
    // ============================

    /// <summary>
    /// 验证 /health/ready 返回低敏就绪状态，包含所有组件的 Pass/Warn/Fail。
    /// </summary>
    [Fact]
    public async Task Should_Return_Ready_Status()
    {
        var result = await _deploymentHealthCheckService.GetReadyAsync();

        result.ShouldNotBeNull();
        result.Checks.Count.ShouldBeGreaterThanOrEqualTo(8);

        // 验证 ready 层消息为低敏格式（不含修复建议）
        foreach (var check in result.Checks)
        {
            check.Name.ShouldNotBeNullOrWhiteSpace();
            check.Message.ShouldNotBeNullOrWhiteSpace();
        }

        // 验证 ready 层不包含 FixSuggestion（该字段在 ready 层不应存在）
        var resultType = result.GetType();
        var checksProp = resultType.GetProperty("Checks");
        var checksList = checksProp!.GetValue(result) as System.Collections.IList;
        var firstCheck = checksList![0]!;
        var fixSuggestionProp = firstCheck.GetType().GetProperty("FixSuggestion");
        fixSuggestionProp.ShouldBeNull("Ready 层不应包含 FixSuggestion 字段");

        // 验证时间戳
        result.GeneratedAt.ShouldBeGreaterThan(DateTime.MinValue);
        result.GeneratedAt.Kind.ShouldBe(DateTimeKind.Utc);

        // 验证 ContainsSensitiveData
        result.ContainsSensitiveData.ShouldBeFalse();
    }

    /// <summary>
    /// 验证 /health/ready 返回的消息仅为低敏摘要（"正常"/"降级"/"不可用"），不含详细诊断。
    /// </summary>
    [Fact]
    public async Task Ready_Should_Only_Contain_LowSensitivity_Messages()
    {
        var result = await _deploymentHealthCheckService.GetReadyAsync();

        foreach (var check in result.Checks)
        {
            // 消息应为低敏格式：组件名 + 状态描述
            check.Message.ShouldMatch(@"^(数据库连接|Redis/分布式缓存|存储卷可写性|FFmpeg|FFprobe|OpenIddict Issuer URL|OpenIddict 应用 Redirect URLs|安全配置检查) (正常|降级|不可用)$");
        }
    }

    /// <summary>
    /// 验证 /health/ready 输出不包含任何敏感信息。
    /// </summary>
    [Fact]
    public async Task Ready_Should_Not_Expose_Sensitive_Data()
    {
        var result = await _deploymentHealthCheckService.GetReadyAsync();

        var allOutputText = string.Join(
            "\n",
            result.Checks.Select(c =>
                $"{c.Name}|{c.Status}|{c.Message}"));

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

        result.ContainsSensitiveData.ShouldBeFalse();
    }

    /// <summary>
    /// 验证 /health/ready 在无认证情况下可访问。
    /// </summary>
    [Fact]
    public async Task Ready_Should_Be_Accessible_Without_Authentication()
    {
        var result = await _deploymentHealthCheckService.GetReadyAsync();

        result.ShouldNotBeNull();
        result.Checks.Count.ShouldBeGreaterThanOrEqualTo(8);
    }

    // ============================
    // 脱敏增强测试
    // ============================

    /// <summary>
    /// 验证 SanitizeErrorMessage 能脱敏 Windows 驱动器绝对路径（不含 ForbiddenSensitiveMarkers 的辅助测试）。
    /// </summary>
    [Fact]
    public void SanitizeErrorMessage_Should_Redact_Windows_Path()
    {
        var message = "File not found at C:\\Users\\testuser\\AppData\\app\\config.json";
        var result = DeploymentHealthCheckService.SanitizeErrorMessage(message);
        result.ShouldNotContain("C:\\Users");
        result.ShouldContain("**路径已脱敏**");
    }

    /// <summary>
    /// 验证 SanitizeErrorMessage 能脱敏 Linux 绝对路径。
    /// </summary>
    [Fact]
    public void SanitizeErrorMessage_Should_Redact_Linux_Path()
    {
        var message = "Binary not found at /var/lib/app/ffmpeg";
        var result = DeploymentHealthCheckService.SanitizeErrorMessage(message);
        result.ShouldNotContain("/var/lib");
        result.ShouldContain("**路径已脱敏**");
    }

    /// <summary>
    /// 验证 SanitizeErrorMessage 能脱敏连接字符串内的密码（使用 Pwd 避免触发 Pass*word 标记提前返回）。
    /// </summary>
    [Fact]
    public void SanitizeErrorMessage_Should_Redact_ConnectionString_Password()
    {
        var message = "Connection failed: Server=db;Port=5432;Uid=admin;Pwd=SuperSecret123!";
        var result = DeploymentHealthCheckService.SanitizeErrorMessage(message);
        result.ShouldNotContain("SuperSecret123");
        result.ShouldContain("**已脱敏**");
    }

    /// <summary>
    /// 验证 SanitizeErrorMessage 能脱敏 JWT token 格式字符串（所有段 ≥10 字符）。
    /// </summary>
    [Fact]
    public void SanitizeErrorMessage_Should_Redact_Jwt_Token()
    {
        var message = "Token validation failed: eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwMTIzNDU2Nzg5MCJ9.dGVzdFRva2VuU2VnbWVudFNpZ25hdHVyZQ";
        var result = DeploymentHealthCheckService.SanitizeErrorMessage(message);
        result.ShouldNotContain("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9");
        result.ShouldContain("**token已脱敏**");
    }

    /// <summary>
    /// 验证 SanitizeErrorMessage 能脱敏 AccessKey（使用标记时不触发 ForbiddenSensitiveMarkers 的值）。
    /// 注意：AccessKeySecret 在 ForbiddenSensitiveMarkers 中导致整体脱敏，此处测试 key 值部分的脱敏。
    /// </summary>
    [Fact]
    public void SanitizeErrorMessage_Should_Redact_AccessKey()
    {
        var message = "OSS access failed: AccessKey=mySecretKey12345 with region cn-hangzhou";
        var result = DeploymentHealthCheckService.SanitizeErrorMessage(message);
        result.ShouldNotContain("mySecretKey12345");
        result.ShouldContain("**AccessKey已脱敏**");
    }

    /// <summary>
    /// 验证 SanitizePath 脱敏包含 ForbiddenSensitiveMarkers 的路径时返回安全摘要。
    /// </summary>
    [Fact]
    public void SanitizePath_Should_Redact_Enitely_When_Contains_Sensitive_Markers()
    {
        var path = "D:\\Devs\\Projects\\Personal\\PrivateCloudDrive\\App_Data\\FileCenter";
        var result = DeploymentHealthCheckService.SanitizePath(path);
        // "PrivateCloudDrive" is in ForbiddenSensitiveMarkers, so entire path is redacted
        result.ShouldBe("路径已脱敏");
    }

    /// <summary>
    /// 验证 SanitizePath 对短路径（≤2 段）不进行截断。
    /// </summary>
    [Fact]
    public void SanitizePath_Should_Keep_Short_Path_Intact()
    {
        var path = "App_Data/FileCenter";
        var result = DeploymentHealthCheckService.SanitizePath(path);
        result.ShouldBe("App_Data/FileCenter");
    }
}
