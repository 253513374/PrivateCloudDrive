using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrivateCloudDrive.Permissions;
using PrivateCloudDrive.Settings;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace PrivateCloudDrive.FileCenter;

public class FileCenterFileUploadService : IFileCenterFileUploadService, ITransientDependency
{
    private const long DefaultMaxUploadFileSizeInBytes = 104857600;
    private const long DefaultUserStorageQuotaInBytes = 10737418240;

    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly ISettingProvider _settingProvider;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IFileCenterBlobStorageService _blobStorageService;
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly FileNodeManager _fileNodeManager;
    private readonly IFileCenterMediaAssetService _mediaAssetService;

    public FileCenterFileUploadService(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ISettingProvider settingProvider,
        IAsyncQueryableExecuter asyncExecuter,
        IFileCenterBlobStorageService blobStorageService,
        IFileNodeRepository fileNodeRepository,
        IRepository<BlobObject, Guid> blobObjectRepository,
        FileNodeManager fileNodeManager,
        IFileCenterMediaAssetService mediaAssetService)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _settingProvider = settingProvider;
        _asyncExecuter = asyncExecuter;
        _blobStorageService = blobStorageService;
        _fileNodeRepository = fileNodeRepository;
        _blobObjectRepository = blobObjectRepository;
        _fileNodeManager = fileNodeManager;
        _mediaAssetService = mediaAssetService;
    }

    [UnitOfWork]
    public virtual async Task<FileNodeDto> UploadSmallFileAsync(
        Guid? parentId,
        string fileName,
        string? contentType,
        Stream stream,
        long size,
        CancellationToken cancellationToken = default)
    {
        var ownerId = GetOwnerId();
        var safeFileName = NormalizeFileName(fileName);

        await EnsureUploadSizeAsync(ownerId, size);
        await _fileNodeManager.EnsureCanCreateAsync(_currentTenant.Id, ownerId, parentId, safeFileName);

        var blobObject = await _blobStorageService.SaveAsync(
            ownerId,
            safeFileName,
            contentType,
            stream,
            size,
            cancellationToken: cancellationToken);

        var fileNode = await _fileNodeManager.CreateFileAsync(
            _currentTenant.Id,
            ownerId,
            parentId,
            safeFileName,
            size,
            contentType,
            blobObject.BlobName);

        await _fileNodeRepository.InsertAsync(fileNode, autoSave: true, cancellationToken);
        await _mediaAssetService.CreatePendingAssetAsync(fileNode);

        return ToDto(fileNode);
    }

    [UnitOfWork]
    public virtual async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = GetOwnerId();
        var fileNode = await _fileNodeManager.GetOwnerFileAsync(_currentTenant.Id, ownerId, id);

        await _fileNodeRepository.DeleteAsync(fileNode, autoSave: true, cancellationToken);
    }

    private Guid GetOwnerId()
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return _currentUser.Id.Value;
    }

    private static string NormalizeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeFileName) ||
            safeFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidFileName)
                .WithData("FileName", fileName);
        }

        return safeFileName;
    }

    private async Task EnsureUploadSizeAsync(Guid ownerId, long size)
    {
        if (size < 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterFileTooLarge)
                .WithData("Size", size);
        }

        var maxUploadFileSize = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.MaxUploadFileSizeInBytes,
            DefaultMaxUploadFileSizeInBytes);

        if (size > maxUploadFileSize)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterFileTooLarge)
                .WithData("Size", size)
                .WithData("MaxSize", maxUploadFileSize);
        }

        var quota = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes,
            DefaultUserStorageQuotaInBytes);

        var usedStorageSize = await GetUsedStorageSizeAsync(ownerId);

        if (usedStorageSize + size > quota)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterStorageQuotaExceeded)
                .WithData("Size", size)
                .WithData("UsedSize", usedStorageSize)
                .WithData("Quota", quota);
        }
    }

    private async Task<long> GetUsedStorageSizeAsync(Guid ownerId)
    {
        var queryable = await _blobObjectRepository.GetQueryableAsync();
        var sizes = await _asyncExecuter.ToListAsync(
            queryable
                .Where(blob => blob.TenantId == _currentTenant.Id && blob.OwnerId == ownerId)
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

    private static FileNodeDto ToDto(FileNode node)
    {
        return new FileNodeDto
        {
            Id = node.Id,
            TenantId = node.TenantId,
            OwnerId = node.OwnerId,
            ParentId = node.ParentId,
            NodeType = node.NodeType,
            Name = node.Name,
            NormalizedName = node.NormalizedName,
            Size = node.Size,
            ContentType = node.ContentType,
            BlobName = node.BlobName,
            IsFavorite = node.IsFavorite,
            CreationTime = node.CreationTime,
            LastModificationTime = node.LastModificationTime
        };
    }
}
