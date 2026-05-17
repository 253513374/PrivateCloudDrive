using System;
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

            result.OverallStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.ApiStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.DatabaseStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.RedisStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.StorageStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.FfmpegStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.FfprobeStatus.ShouldBe(FileCenterSystemHealthStatus.Healthy);
            result.StorageProvider.ShouldBe(FileCenterStorageProviderNames.FileSystem);
            result.StorageLocationDescription.ShouldBe("服务器本机/容器文件系统（FileCenter:StorageRootPath 对应目录）");
            result.BackupScopeDescription.ShouldBe("请同时备份数据库、FileCenter 存储目录和部署 .env/密钥配置；仅备份 App 本机数据不能恢复服务器文件。");
            result.PrivacyBoundaryDescription.ShouldBe("文件保存到当前连接的私有后端；服务器管理员和具备存储访问权限的人可能接触原始文件，分享链接会扩大访问边界。");
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
            result.Diagnostics.ShouldContain("FFmpeg 已配置");
            result.Diagnostics.ShouldContain("FFprobe 已配置");
        });
    }

    /// <summary>
    /// 验证对象存储模式下恢复边界提示 OSS Bucket/Object 数据，而不是误导用户备份本地目录。
    /// </summary>
    [Fact]
    public async Task Should_Return_Provider_Aware_Backup_Scope_For_Aliyun_Oss()
    {
        var originalProvider = _configuration["FileCenter:StorageProvider"];
        var originalBucketName = _configuration["FileCenter:AliyunOss:BucketName"];
        var userId = Guid.NewGuid();

        try
        {
            _configuration["FileCenter:StorageProvider"] = FileCenterStorageProviderNames.AliyunOss;
            _configuration["FileCenter:AliyunOss:BucketName"] = "private-backup-bucket";

            await WithCurrentUserAsync(userId, async () =>
            {
                var result = await _systemHealthAppService.GetSummaryAsync();

                result.StorageProvider.ShouldBe(FileCenterStorageProviderNames.AliyunOss);
                result.StorageLocationDescription.ShouldBe("阿里云 OSS 私有 Bucket（名称不在 App 展示）");
                result.BackupScopeDescription.ShouldContain("阿里云 OSS 私有 Bucket/Object 数据");
                result.BackupScopeDescription.ShouldNotContain("FileCenter 存储目录");
                result.StorageLocationDescription.ShouldNotContain("private-backup-bucket");
            });
        }
        finally
        {
            _configuration["FileCenter:StorageProvider"] = originalProvider;
            _configuration["FileCenter:AliyunOss:BucketName"] = originalBucketName;
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
