using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterFileUploadServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid> _blobObjectRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public EfCoreFileCenterFileUploadServiceTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _blobObjectRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Should_Upload_Small_File_And_Create_FileNode_And_BlobObject()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("uploaded text");

        await WithCurrentUserAsync(userId, async () =>
        {
            var folder = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Uploads" });

            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                folder.Id,
                "note.txt",
                "text/plain",
                stream,
                content.Length);

            fileNode.ParentId.ShouldBe(folder.Id);
            fileNode.Name.ShouldBe("note.txt");
            fileNode.NodeType.ShouldBe(PrivateCloudDrive.FileCenter.FileNodeType.File);
            fileNode.Size.ShouldBe(content.Length);
            fileNode.ContentType.ShouldBe("text/plain");
            fileNode.BlobName.ShouldNotBeNullOrWhiteSpace();

            var list = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    ParentId = folder.Id,
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            list.TotalCount.ShouldBe(1);
            list.Items.Single().Name.ShouldBe("note.txt");

            var blobObject = await WithUnitOfWorkAsync(async () =>
                await _blobObjectRepository.SingleAsync(blob => blob.BlobName == fileNode.BlobName));

            blobObject.OwnerId.ShouldBe(userId);
            blobObject.FileName.ShouldBe("note.txt");
            blobObject.Size.ShouldBe(content.Length);
        });
    }

    [Fact]
    public async Task Should_Reject_Too_Large_File()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("too large by declared size");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var stream = new MemoryStream(content);

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    fileName: "too-large.bin",
                    contentType: "application/octet-stream",
                    stream,
                    size: 104857601);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterFileTooLarge);
        });
    }

    [Fact]
    public async Task Should_Reject_Duplicate_File_Name_In_Folder()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("duplicate");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using (var firstStream = new MemoryStream(content))
            {
                await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    fileName: "same.txt",
                    contentType: "text/plain",
                    firstStream,
                    content.Length);
            }

            await using var secondStream = new MemoryStream(content);

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    fileName: "SAME.txt",
                    contentType: "text/plain",
                    secondStream,
                    content.Length);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeAlreadyExists);
        });
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "file-upload-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
