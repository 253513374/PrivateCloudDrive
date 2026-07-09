using System;
using System.IO;
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

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 存储配置只读应用服务，管理员可查看当前存储后端配置和容量信息。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class StorageConfigAppService : PrivateCloudDriveAppService, IStorageConfigAppService
{
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ISettingProvider _settingProvider;
    private readonly IConfiguration _configuration;

    public StorageConfigAppService(
        IRepository<BlobObject, Guid> blobObjectRepository,
        IAsyncQueryableExecuter asyncExecuter,
        ISettingProvider settingProvider,
        IConfiguration configuration)
    {
        _blobObjectRepository = blobObjectRepository;
        _asyncExecuter = asyncExecuter;
        _settingProvider = settingProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// 获取存储配置信息（只读）。
    /// </summary>
    public virtual async Task<StorageConfigDto> GetAsync()
    {
        var storageProvider = FileCenterStorageProviderNames.Normalize(
            _configuration["FileCenter:StorageProvider"]);

        var storagePath = ResolveSanitizedPath(storageProvider);
        var (totalBytes, availableBytes) = ResolveDiskSpace(storageProvider);

        var usedBytes = await GetTotalUsedStorageSizeAsync();

        var maxSingleFileSize = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.MaxUploadFileSizeInBytes,
            0);

        return new StorageConfigDto
        {
            StorageProvider = storageProvider,
            StoragePath = storagePath,
            TotalBytes = totalBytes,
            UsedBytes = usedBytes,
            AvailableBytes = availableBytes,
            MaxSingleFileSize = maxSingleFileSize
        };
    }

    private string ResolveSanitizedPath(string storageProvider)
    {
        if (storageProvider == FileCenterStorageProviderNames.AliyunOss)
        {
            return "对象存储（OSS）";
        }

        var storageRootPath = FileCenterBlobStoragePath.GetFullPath(_configuration);
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return "未配置";
        }

        // 脱敏：仅显示盘符或最后一个目录名
        try
        {
            var fullPath = Path.GetFullPath(storageRootPath);
            var root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrWhiteSpace(root) && fullPath.Length > root.Length)
            {
                var lastDir = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return $"{root}[...]\\{lastDir}";
            }

            return root ?? "本地存储";
        }
        catch
        {
            return "本地存储";
        }
    }

    private (long TotalBytes, long AvailableBytes) ResolveDiskSpace(string storageProvider)
    {
        if (storageProvider == FileCenterStorageProviderNames.AliyunOss)
        {
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
            return (0, 0);
        }

        try
        {
            var drive = new DriveInfo(probePath);
            return (drive.TotalSize, drive.AvailableFreeSpace);
        }
        catch
        {
            return (0, 0);
        }
    }

    private async Task<long> GetTotalUsedStorageSizeAsync()
    {
        var queryable = await _blobObjectRepository.GetQueryableAsync();
        var sizes = await _asyncExecuter.ToListAsync(
            queryable
                .Where(blob => blob.TenantId == CurrentTenant.Id)
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
}
