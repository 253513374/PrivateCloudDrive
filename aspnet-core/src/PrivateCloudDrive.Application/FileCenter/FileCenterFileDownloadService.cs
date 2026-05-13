using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件下载和缩略图读取应用服务。
/// 只允许当前用户读取自己拥有的文件，并通过 Blob 容器返回流式内容。
/// </summary>
public class FileCenterFileDownloadService : IFileCenterFileDownloadService, ITransientDependency
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly IFileCenterBlobContentReader _blobContentReader;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IRepository<MediaAsset, Guid> _mediaAssetRepository;
    private readonly FileNodeManager _fileNodeManager;

    /// <summary>
    /// 初始化 <see cref="FileCenterFileDownloadService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterFileDownloadService(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IFileCenterBlobContentReader blobContentReader,
        IRepository<BlobObject, Guid> blobObjectRepository,
        IRepository<MediaAsset, Guid> mediaAssetRepository,
        FileNodeManager fileNodeManager)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _blobContentReader = blobContentReader;
        _blobObjectRepository = blobObjectRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _fileNodeManager = fileNodeManager;
    }

    /// <summary>
    /// 获取原始文件下载流和响应元数据。控制器可基于该信息支持 Range 下载和浏览器保存文件名。
    /// </summary>
    [UnitOfWork]
    public virtual async Task<FileDownloadInfo> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await GetDownloadAsync(id, range: null, cancellationToken);
    }

    [UnitOfWork]
    public virtual async Task<FileDownloadInfo> GetDownloadAsync(
        Guid id,
        FileDownloadRangeRequest? range,
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

        var normalizedRange = range?.Normalize(blobObject.Size);
        var stream = normalizedRange == null
            ? await _blobContentReader.OpenReadAsync(blobObject.BlobName, cancellationToken)
            : await _blobContentReader.OpenReadRangeAsync(
                blobObject.BlobName,
                normalizedRange.Start,
                normalizedRange.End,
                cancellationToken);

        return new FileDownloadInfo
        {
            FileName = fileNode.Name,
            ContentType = blobObject.ContentType ?? fileNode.ContentType ?? DefaultContentType,
            Size = normalizedRange?.Length ?? blobObject.Size,
            TotalSize = blobObject.Size,
            Range = normalizedRange,
            Content = stream
        };
    }

    /// <summary>
    /// 获取媒体缩略图下载流。缩略图不存在时抛出业务异常，由上层返回明确错误。
    /// </summary>
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

        var stream = await _blobContentReader.OpenReadAsync(blobObject.BlobName, cancellationToken);

        return new FileDownloadInfo
        {
            FileName = $"{Path.GetFileNameWithoutExtension(fileNode.Name)}.thumbnail.jpg",
            ContentType = blobObject.ContentType ?? "image/jpeg",
            Size = blobObject.Size,
            TotalSize = blobObject.Size,
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
