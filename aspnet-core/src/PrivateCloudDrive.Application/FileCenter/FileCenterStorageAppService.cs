using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using PrivateCloudDrive.Settings;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Settings;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心容量应用服务，负责为客户端设置页提供当前用户的容量使用摘要。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterStorageAppService : FileCenterAppService, IFileCenterStorageAppService
{
    private const long DefaultUserStorageQuotaInBytes = 10737418240;

    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ISettingProvider _settingProvider;

    /// <summary>
    /// 初始化 <see cref="FileCenterStorageAppService"/> 的新实例。
    /// </summary>
    public FileCenterStorageAppService(
        IRepository<BlobObject, Guid> blobObjectRepository,
        IAsyncQueryableExecuter asyncExecuter,
        ISettingProvider settingProvider)
    {
        _blobObjectRepository = blobObjectRepository;
        _asyncExecuter = asyncExecuter;
        _settingProvider = settingProvider;
    }

    /// <summary>
    /// 获取当前用户的容量使用摘要，不暴露底层存储路径或 Blob 物理位置。
    /// </summary>
    public virtual async Task<StorageUsageDto> GetUsageAsync()
    {
        var ownerId = GetOwnerId();
        var usedBytes = await GetUsedStorageSizeAsync(ownerId);
        var quotaBytes = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes,
            DefaultUserStorageQuotaInBytes);

        var isQuotaConfigured = quotaBytes > 0;
        var remainingBytes = isQuotaConfigured
            ? Math.Max(quotaBytes - usedBytes, 0)
            : 0;
        var usagePercent = isQuotaConfigured
            ? Math.Round((decimal)usedBytes / quotaBytes * 100, 2, MidpointRounding.AwayFromZero)
            : 0;
        var maxSingleFileSize = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.MaxUploadFileSizeInBytes,
            0);

        return new StorageUsageDto
        {
            UsedBytes = usedBytes,
            QuotaBytes = quotaBytes,
            RemainingBytes = remainingBytes,
            UsagePercent = usagePercent,
            IsQuotaConfigured = isQuotaConfigured,
            MaxSingleFileSize = maxSingleFileSize
        };
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
            throw new AbpAuthorizationException("Current user is required for FileCenter storage usage operations.");
        }

        return CurrentUser.Id.Value;
    }
}
