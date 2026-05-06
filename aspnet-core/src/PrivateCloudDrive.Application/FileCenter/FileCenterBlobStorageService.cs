using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterBlobStorageService
{
    Task<BlobObject> SaveAsync(
        Guid ownerId,
        string fileName,
        string? contentType,
        Stream stream,
        long size,
        string? hash = null,
        CancellationToken cancellationToken = default);
}

public class FileCenterBlobStorageService : IFileCenterBlobStorageService, ITransientDependency
{
    private readonly IBlobContainer<FileCenterBlobContainer> _blobContainer;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IGuidGenerator _guidGenerator;

    public FileCenterBlobStorageService(
        IBlobContainer<FileCenterBlobContainer> blobContainer,
        IRepository<BlobObject, Guid> blobObjectRepository,
        ICurrentTenant currentTenant,
        IGuidGenerator guidGenerator)
    {
        _blobContainer = blobContainer;
        _blobObjectRepository = blobObjectRepository;
        _currentTenant = currentTenant;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public virtual async Task<BlobObject> SaveAsync(
        Guid ownerId,
        string fileName,
        string? contentType,
        Stream stream,
        long size,
        string? hash = null,
        CancellationToken cancellationToken = default)
    {
        var blobId = _guidGenerator.Create();
        var blobName = CreateBlobName(_currentTenant.Id, ownerId, blobId, fileName);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await _blobContainer.SaveAsync(blobName, stream, overrideExisting: false, cancellationToken);

        var blobObject = BlobObject.Create(
            blobId,
            _currentTenant.Id,
            ownerId,
            blobName,
            fileName,
            size,
            contentType,
            hash);

        await _blobObjectRepository.InsertAsync(blobObject, autoSave: true, cancellationToken);

        return blobObject;
    }

    private static string CreateBlobName(Guid? tenantId, Guid ownerId, Guid blobId, string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (extension.Length > 32)
        {
            extension = string.Empty;
        }

        var tenantPart = tenantId?.ToString("N") ?? "host";

        return $"{tenantPart}/{ownerId:N}/{blobId:N}{extension}";
    }
}
