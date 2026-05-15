using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using PrivateCloudDrive.Permissions;
using PrivateCloudDrive.Settings;
using Volo.Abp.Authorization;
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
    private readonly ISettingProvider _settingProvider;
    private readonly IConfiguration _configuration;
    private readonly IClock _clock;

    /// <summary>
    /// 初始化 <see cref="FileCenterSystemHealthAppService"/> 的新实例。
    /// </summary>
    public FileCenterSystemHealthAppService(
        IRepository<BlobObject, Guid> blobObjectRepository,
        IAsyncQueryableExecuter asyncExecuter,
        ISettingProvider settingProvider,
        IConfiguration configuration,
        IClock clock)
    {
        _blobObjectRepository = blobObjectRepository;
        _asyncExecuter = asyncExecuter;
        _settingProvider = settingProvider;
        _configuration = configuration;
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
        var usedBytes = await GetUsedStorageSizeAsync(ownerId);
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
            OverallStatus = ResolveOverallStatus(storageStatus),
            ApiStatus = FileCenterSystemHealthStatus.Healthy,
            StorageStatus = storageStatus,
            StorageProvider = storageProvider,
            StorageUsedBytes = usedBytes,
            StorageQuotaBytes = quotaBytes,
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

    private static FileCenterSystemHealthStatus ResolveOverallStatus(FileCenterSystemHealthStatus storageStatus)
    {
        return storageStatus == FileCenterSystemHealthStatus.Unhealthy
            ? FileCenterSystemHealthStatus.Unhealthy
            : FileCenterSystemHealthStatus.Healthy;
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
