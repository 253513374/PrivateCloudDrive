using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace PrivateCloudDrive.FileCenter;

public class FileCenterFileDownloadService : IFileCenterFileDownloadService, ITransientDependency
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly IBlobContainer<FileCenterBlobContainer> _blobContainer;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly FileNodeManager _fileNodeManager;

    public FileCenterFileDownloadService(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IBlobContainer<FileCenterBlobContainer> blobContainer,
        IRepository<BlobObject, Guid> blobObjectRepository,
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        FileNodeManager fileNodeManager)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _blobContainer = blobContainer;
        _blobObjectRepository = blobObjectRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _fileNodeManager = fileNodeManager;
    }

    [UnitOfWork]
    public virtual async Task<FileDownloadInfo> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = GetOwnerId();
        var fileNode = await _fileNodeManager.GetOwnerFileAsync(_currentTenant.Id, ownerId, id);

        if (fileNode.BlobName.IsNullOrWhiteSpace())
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterBlobObjectNotFound)
                .WithData("Id", id);
        }

        var blobObject = await _blobObjectRepository.FirstOrDefaultAsync(
            blob =>
                blob.TenantId == _currentTenant.Id &&
                blob.OwnerId == ownerId &&
                blob.BlobName == fileNode.BlobName,
            cancellationToken: cancellationToken);

        if (blobObject == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterBlobObjectNotFound)
                .WithData("BlobName", fileNode.BlobName);
        }

        var stream = await _blobContainer.GetAsync(blobObject.BlobName, cancellationToken);

        return new FileDownloadInfo
        {
            FileName = fileNode.Name,
            ContentType = blobObject.ContentType ?? fileNode.ContentType ?? DefaultContentType,
            Size = blobObject.Size,
            Content = stream
        };
    }

    [UnitOfWork]
    public virtual async Task<FileDownloadInfo> GetThumbnailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = GetOwnerId();
        var fileNode = await _fileNodeManager.GetOwnerFileAsync(_currentTenant.Id, ownerId, id);
        var mediaAsset = await _mediaAssetRepository.FirstOrDefaultAsync(
            asset =>
                asset.TenantId == _currentTenant.Id &&
                asset.OwnerId == ownerId &&
                asset.FileNodeId == fileNode.Id,
            cancellationToken: cancellationToken);

        if (mediaAsset?.ThumbnailBlobObjectId == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterThumbnailNotFound)
                .WithData("Id", id);
        }

        var blobObject = await _blobObjectRepository.FirstOrDefaultAsync(
            blob =>
                blob.Id == mediaAsset.ThumbnailBlobObjectId.Value &&
                blob.TenantId == _currentTenant.Id &&
                blob.OwnerId == ownerId,
            cancellationToken: cancellationToken);

        if (blobObject == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterThumbnailNotFound)
                .WithData("ThumbnailBlobObjectId", mediaAsset.ThumbnailBlobObjectId.Value);
        }

        var stream = await _blobContainer.GetAsync(blobObject.BlobName, cancellationToken);

        return new FileDownloadInfo
        {
            FileName = $"{Path.GetFileNameWithoutExtension(fileNode.Name)}.thumbnail.jpg",
            ContentType = blobObject.ContentType ?? "image/jpeg",
            Size = blobObject.Size,
            Content = stream
        };
    }

    private Guid GetOwnerId()
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return _currentUser.Id.Value;
    }
}
