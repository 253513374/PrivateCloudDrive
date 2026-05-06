using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterBlobStorageServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterBlobStorageService _blobStorageService;
    private readonly IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid> _blobObjectRepository;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterBlobStoragePathProvider _storagePathProvider;

    public EfCoreFileCenterBlobStorageServiceTests()
    {
        _blobStorageService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterBlobStorageService>();
        _blobObjectRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid>>();
        _storagePathProvider = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterBlobStoragePathProvider>();
    }

    [Fact]
    public async Task Should_Save_Physical_File_And_Create_BlobObject()
    {
        var ownerId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("hello private cloud drive");
        var storageRootPath = _storagePathProvider.GetStorageRootPath();
        var filesBeforeSave = GetFiles(storageRootPath);

        await using var stream = new MemoryStream(content);

        var blobObject = await _blobStorageService.SaveAsync(
            ownerId,
            "hello.txt",
            "text/plain",
            stream,
            content.Length,
            hash: "sha256-test");

        var savedBlobObject = await WithUnitOfWorkAsync(async () =>
            await _blobObjectRepository.GetAsync(blobObject.Id));

        savedBlobObject.OwnerId.ShouldBe(ownerId);
        savedBlobObject.BlobName.ShouldBe(blobObject.BlobName);
        savedBlobObject.FileName.ShouldBe("hello.txt");
        savedBlobObject.ContentType.ShouldBe("text/plain");
        savedBlobObject.Size.ShouldBe(content.Length);
        savedBlobObject.Hash.ShouldBe("sha256-test");

        var filesAfterSave = GetFiles(storageRootPath);
        var newlyCreatedFiles = filesAfterSave
            .Where(file => !filesBeforeSave.Contains(file))
            .ToList();

        newlyCreatedFiles.ShouldNotBeEmpty();
        newlyCreatedFiles
            .Any(file => File.ReadAllBytes(file).SequenceEqual(content))
            .ShouldBeTrue();
    }

    private static HashSet<string> GetFiles(string storageRootPath)
    {
        if (!Directory.Exists(storageRootPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return Directory
            .EnumerateFiles(storageRootPath, "*", SearchOption.AllDirectories)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
