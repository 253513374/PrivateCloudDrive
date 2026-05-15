using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PrivateCloudDrive.Permissions;
using PrivateCloudDrive.Settings;
using Volo.Abp.Authorization;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Settings;
using Volo.Abp.Timing;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心系统健康应用服务，负责为客户端设置页提供后端与存储运行摘要。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterSystemHealthAppService : FileCenterAppService, IFileCenterSystemHealthAppService
{
    private const long DefaultUserStorageQuotaInBytes = 10737418240;

    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IDistributedCache<FileCenterSystemHealthCacheItem, string> _cache;
    private readonly ISettingProvider _settingProvider;
    private readonly IConfiguration _configuration;
    private readonly FileCenterMediaProcessingOptions _mediaProcessingOptions;
    private readonly IClock _clock;

    /// <summary>
    /// 初始化 <see cref="FileCenterSystemHealthAppService"/> 的新实例。
    /// </summary>
    public FileCenterSystemHealthAppService(
        IRepository<BlobObject, Guid> blobObjectRepository,
        IAsyncQueryableExecuter asyncExecuter,
        IDistributedCache<FileCenterSystemHealthCacheItem, string> cache,
        ISettingProvider settingProvider,
        IConfiguration configuration,
        IOptions<FileCenterMediaProcessingOptions> mediaProcessingOptions,
        IClock clock)
    {
        _blobObjectRepository = blobObjectRepository;
        _asyncExecuter = asyncExecuter;
        _cache = cache;
        _settingProvider = settingProvider;
        _configuration = configuration;
        _mediaProcessingOptions = mediaProcessingOptions.Value;
        _clock = clock;
    }

    /// <summary>
    /// 获取当前用户可见的文件中心系统健康摘要。
    /// </summary>
    public virtual async Task<FileCenterSystemHealthDto> GetSummaryAsync()
    {
        var ownerId = GetOwnerId();
        var diagnostics = new List<string> { "API 可访问" };
        var storageProvider = FileCenterStorageProviderNames.Normalize(_configuration["FileCenter:StorageProvider"]);
        var storageStatus = ResolveStorageStatus(storageProvider, diagnostics);
        var (storageDiskAvailableBytes, storageDiskTotalBytes) = ResolveStorageDiskSpace(storageProvider, diagnostics);
        var ffmpegStatus = ResolveToolStatus(_mediaProcessingOptions.FfmpegPath, "FFmpeg", diagnostics);
        var ffprobeStatus = ResolveToolStatus(_mediaProcessingOptions.FfprobePath, "FFprobe", diagnostics);
        var usedBytes = await GetUsedStorageSizeAsync(ownerId);
        var databaseStatus = FileCenterSystemHealthStatus.Healthy;
        diagnostics.Add("数据库可访问");
        var redisStatus = await ResolveRedisStatusAsync(diagnostics);
        var quotaBytes = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes,
            DefaultUserStorageQuotaInBytes);
        var isQuotaConfigured = quotaBytes > 0;

        if (!isQuotaConfigured)
        {
            diagnostics.Add("用户容量配额未启用");
        }

        return new FileCenterSystemHealthDto
        {
            OverallStatus = ResolveOverallStatus(databaseStatus, redisStatus, storageStatus, ffmpegStatus, ffprobeStatus),
            ApiStatus = FileCenterSystemHealthStatus.Healthy,
            DatabaseStatus = databaseStatus,
            RedisStatus = redisStatus,
            StorageStatus = storageStatus,
            FfmpegStatus = ffmpegStatus,
            FfprobeStatus = ffprobeStatus,
            StorageProvider = storageProvider,
            StorageUsedBytes = usedBytes,
            StorageQuotaBytes = quotaBytes,
            StorageDiskAvailableBytes = storageDiskAvailableBytes,
            StorageDiskTotalBytes = storageDiskTotalBytes,
            IsQuotaConfigured = isQuotaConfigured,
            GeneratedAt = _clock.Now,
            Diagnostics = diagnostics
        };
    }

    private FileCenterSystemHealthStatus ResolveStorageStatus(
        string storageProvider,
        ICollection<string> diagnostics)
    {
        if (storageProvider == FileCenterStorageProviderNames.AliyunOss)
        {
            diagnostics.Add("存储后端 AliyunOss 已配置");
            return FileCenterSystemHealthStatus.Healthy;
        }

        var storageRootPath = FileCenterBlobStoragePath.GetFullPath(_configuration);
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            diagnostics.Add("本地文件系统存储路径未配置");
            return FileCenterSystemHealthStatus.Unhealthy;
        }

        diagnostics.Add($"存储后端 {FileCenterStorageProviderNames.FileSystem} 已配置");
        return FileCenterSystemHealthStatus.Healthy;
    }

    private (long AvailableBytes, long TotalBytes) ResolveStorageDiskSpace(
        string storageProvider,
        ICollection<string> diagnostics)
    {
        if (storageProvider == FileCenterStorageProviderNames.AliyunOss)
        {
            diagnostics.Add("对象存储不适用本地磁盘空间");
            return (0, 0);
        }

        var storageRootPath = FileCenterBlobStoragePath.GetFullPath(_configuration);
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return (0, 0);
        }

        var probePath = Directory.Exists(storageRootPath)
            ? storageRootPath
            : Path.GetPathRoot(Path.GetFullPath(storageRootPath));

        if (string.IsNullOrWhiteSpace(probePath))
        {
            diagnostics.Add("存储磁盘空间不可读取");
            return (0, 0);
        }

        var drive = new DriveInfo(probePath);
        diagnostics.Add("存储磁盘空间可读取");
        return (drive.AvailableFreeSpace, drive.TotalSize);
    }

    private static FileCenterSystemHealthStatus ResolveOverallStatus(
        params FileCenterSystemHealthStatus[] componentStatuses)
    {
        if (componentStatuses.Any(status => status == FileCenterSystemHealthStatus.Unhealthy))
        {
            return FileCenterSystemHealthStatus.Unhealthy;
        }

        return componentStatuses.Any(status => status == FileCenterSystemHealthStatus.Degraded)
            ? FileCenterSystemHealthStatus.Degraded
            : FileCenterSystemHealthStatus.Healthy;
    }

    private static FileCenterSystemHealthStatus ResolveToolStatus(
        string? executablePath,
        string displayName,
        ICollection<string> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            diagnostics.Add($"{displayName} 未配置");
            return FileCenterSystemHealthStatus.Degraded;
        }

        diagnostics.Add($"{displayName} 已配置");
        return FileCenterSystemHealthStatus.Healthy;
    }

    private async Task<FileCenterSystemHealthStatus> ResolveRedisStatusAsync(ICollection<string> diagnostics)
    {
        var cacheKey = $"system-health:{Guid.NewGuid():N}";
        var item = new FileCenterSystemHealthCacheItem
        {
            ProbeId = cacheKey,
            CreatedAt = DateTime.UtcNow
        };

        await _cache.SetAsync(
            cacheKey,
            item,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

        var restored = await _cache.GetAsync(cacheKey);
        await _cache.RemoveAsync(cacheKey);

        if (restored?.ProbeId != cacheKey)
        {
            diagnostics.Add("Redis/分布式缓存探针读写不一致");
            return FileCenterSystemHealthStatus.Degraded;
        }

        diagnostics.Add("Redis/分布式缓存可访问");
        return FileCenterSystemHealthStatus.Healthy;
    }

    private async Task<long> GetUsedStorageSizeAsync(Guid ownerId)
    {
        var queryable = await _blobObjectRepository.GetQueryableAsync();
        var sizes = await _asyncExecuter.ToListAsync(
            queryable
                .Where(blob => blob.TenantId == CurrentTenant.Id && blob.OwnerId == ownerId)
                .Select(blob => blob.Size));

        return sizes.Sum();
    }

    private async Task<long> GetLongSettingAsync(string name, long defaultValue)
    {
        var value = await _settingProvider.GetOrNullAsync(name);

        return long.TryParse(value, out var parsedValue)
            ? parsedValue
            : defaultValue;
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter system health operations.");
        }

        return CurrentUser.Id.Value;
    }
}

/// <summary>
/// 文件中心系统健康缓存探针，仅用于验证分布式缓存读写链路。
/// </summary>
public class FileCenterSystemHealthCacheItem
{
    public string ProbeId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
