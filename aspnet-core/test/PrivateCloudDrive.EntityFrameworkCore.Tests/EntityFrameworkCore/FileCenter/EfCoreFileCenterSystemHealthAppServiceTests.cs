using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using PrivateCloudDrive.FileCenter;
using Shouldly;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 验证文件中心系统健康摘要，确保移动端设置页能展示后端与存储运行状态。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterSystemHealthAppServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IFileCenterSystemHealthAppService _systemHealthAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly FileCenterMediaProcessingOptions _mediaProcessingOptions;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterSystemHealthAppServiceTests"/> 的新实例。
    /// </summary>
    public EfCoreFileCenterSystemHealthAppServiceTests()
    {
        _systemHealthAppService = GetRequiredService<IFileCenterSystemHealthAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _mediaProcessingOptions = GetRequiredService<IOptions<FileCenterMediaProcessingOptions>>().Value;
        _configuration = GetRequiredService<IConfiguration>();
    }

    /// <summary>
    /// 验证系统健康摘要包含安全可展示的后端状态、存储 Provider 与当前用户容量信息。
    /// </summary>
    [Fact]
    public async Task Should_Return_System_Health_Summary_For_Current_User()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var result = await _systemHealthAppService.GetSummaryAsync();

            // 整体状态：如果媒体工具不可执行则为 Degraded，否则为 Healthy
            // 核心组件（API/DB/Redis/Storage）始终为 Healthy
            result.OverallStatus.ShouldBeOneOf(
                FileCenterSystemHealthStatus.Healthy,
                FileCenterSystemHealthStatus.Degraded);
            result.ApiStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.DatabaseStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.RedisStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.StorageStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            // FFmpeg/FFprobe 状态取决于测试环境是否安装了相应工具
            // 存在时应为 Healthy，不存在时应为 Degraded（已配置但不可执行）
            result.FfmpegStatus.ShouldBeOneOf(
                FileCenterSystemHealthStatus.Healthy,
                FileCenterSystemHealthStatus.Degraded);
            result.FfprobeStatus.ShouldBeOneOf(
                FileCenterSystemHealthStatus.Healthy,
                FileCenterSystemHealthStatus.Degraded);
            result.StorageProvider.ShouldBe(FileCenterStorageProviderNames.FileSystem);
            result.StorageLocationDescription.ShouldBe("文件保存在当前私有服务器管理的文件存储中。");
            result.BackupScopeDescription.ShouldBe("请同时备份数据库、文件存储内容和部署密钥配置；手机 App 本机缓存不能单独恢复服务器文件。");
            result.PrivacyBoundaryDescription.ShouldBe("文件保存到当前连接的私有后端；服务器管理员和具备存储访问权限的人可能接触原始文件，分享链接会扩大访问边界。");
            AssertNoInternalStorageDetails(result);
            result.StorageUsedBytes.ShouldBe(0);
            result.StorageQuotaBytes.ShouldBeGreaterThan(0);
            result.StorageDiskAvailableBytes.ShouldBeGreaterThan(0);
            result.StorageDiskTotalBytes.ShouldBeGreaterThan(0);
            result.GeneratedAt.ShouldBeGreaterThan(DateTime.MinValue);
            result.Diagnostics.ShouldContain("API 可访问");
            result.Diagnostics.ShouldContain("数据库可访问");
            result.Diagnostics.ShouldContain("Redis/分布式缓存可访问");
            result.Diagnostics.ShouldContain("存储后端 FileSystem 已配置");
            result.Diagnostics.ShouldContain("存储磁盘空间可读取");
            // FFmpeg/FFprobe 诊断信息取决于工具是否可执行
            var ffmpegDiagnostic = result.Diagnostics.FirstOrDefault(d => d.StartsWith("FFmpeg"));
            var ffprobeDiagnostic = result.Diagnostics.FirstOrDefault(d => d.StartsWith("FFprobe"));
            ffmpegDiagnostic.ShouldNotBeNull();
            ffprobeDiagnostic.ShouldNotBeNull();
            // 可接受："FFmpeg 可执行"、"FFmpeg 已配置但无法执行"、"FFmpeg 已配置但返回错误代码..."
            ffmpegDiagnostic.ShouldMatch("FFmpeg (可执行|已配置)");
            ffprobeDiagnostic.ShouldMatch("FFprobe (可执行|已配置)");
        });
    }

    /// <summary>
    /// 验证对象存储模式下恢复边界提示对象存储安全摘要，而不是暴露 Bucket/Object 等内部诊断字样。
    /// </summary>
    [Fact]
    public async Task Should_Return_Provider_Aware_Backup_Scope_For_Aliyun_Oss()
    {
        var originalProvider = _configuration["FileCenter:StorageProvider"];
        var originalBucketName = _configuration["FileCenter:AliyunOss:BucketName"];
        var originalAccessKeyId = _configuration["FileCenter:AliyunOss:AccessKeyId"];
        var userId = Guid.NewGuid();

        try
        {
            _configuration["FileCenter:StorageProvider"] = FileCenterStorageProviderNames.AliyunOss;
            _configuration["FileCenter:AliyunOss:BucketName"] = "private-backup-bucket";
            _configuration["FileCenter:AliyunOss:AccessKeyId"] = "test-access-key";

            await WithCurrentUserAsync(userId, async () =>
            {
                var result = await _systemHealthAppService.GetSummaryAsync();

                result.StorageProvider.ShouldBe(FileCenterStorageProviderNames.AliyunOss);
                result.StorageLocationDescription.ShouldBe("文件保存在当前私有服务器配置的私有对象存储中；对象存储名称和访问密钥不会在 App 展示。");
                result.BackupScopeDescription.ShouldBe("请同时备份数据库、文件存储内容和部署密钥配置；手机 App 本机缓存不能单独恢复服务器文件。");
                AssertNoInternalStorageDetails(result);
            });
        }
        finally
        {
            _configuration["FileCenter:StorageProvider"] = originalProvider;
            _configuration["FileCenter:AliyunOss:BucketName"] = originalBucketName;
            _configuration["FileCenter:AliyunOss:AccessKeyId"] = originalAccessKeyId;
        }
    }

    /// <summary>
    /// 验证媒体工具未配置时系统健康摘要降级但不暴露本机路径。
    /// </summary>
    [Fact]
    public async Task Should_Degrade_When_Media_Tools_Are_Not_Configured()
    {
        var originalFfmpegPath = _mediaProcessingOptions.FfmpegPath;
        var originalFfprobePath = _mediaProcessingOptions.FfprobePath;

        _mediaProcessingOptions.FfmpegPath = string.Empty;
        _mediaProcessingOptions.FfprobePath = string.Empty;

        try
        {
            var userId = Guid.NewGuid();

            await WithCurrentUserAsync(userId, async () =>
            {
                var result = await _systemHealthAppService.GetSummaryAsync();

                result.OverallStatus.ShouldBe(FileCenterSystemHealthStatus.Degraded);
                result.FfmpegStatus.ShouldBe(FileCenterSystemHealthStatus.Degraded);
                result.FfprobeStatus.ShouldBe(FileCenterSystemHealthStatus.Degraded);
                result.Diagnostics.ShouldContain("FFmpeg 未配置");
                result.Diagnostics.ShouldContain("FFprobe 未配置");
            });
        }
        finally
        {
            _mediaProcessingOptions.FfmpegPath = originalFfmpegPath;
            _mediaProcessingOptions.FfprobePath = originalFfprobePath;
        }
    }

    /// <summary>
    /// 验证数据库探针通过轻量查询验证数据库可读写。
    /// </summary>
    [Fact]
    public async Task Should_Probe_Database_Readability()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var result = await _systemHealthAppService.GetSummaryAsync();

            result.DatabaseStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.Diagnostics.ShouldContain("数据库可访问");
        });
    }

    private static void AssertNoInternalStorageDetails(FileCenterSystemHealthDto result)
    {
        var appVisibleText = string.Join(
            "\n",
            new[]
            {
                result.StorageLocationDescription,
                result.BackupScopeDescription,
                result.PrivacyBoundaryDescription
            }.Concat(result.Diagnostics));

        var forbiddenMarkers = new[]
        {
            "FileCenter:",
            "StorageRootPath",
            "Bucket",
            "Object",
            "private-backup-bucket",
            "test-access-key",
            "AccessKey",
            "连接字符串",
            ".env",
            "raw exception"
        };

        foreach (var marker in forbiddenMarkers)
        {
            appVisibleText.ShouldNotContain(marker, Case.Insensitive);
        }
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, $"health-{userId:N}")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
