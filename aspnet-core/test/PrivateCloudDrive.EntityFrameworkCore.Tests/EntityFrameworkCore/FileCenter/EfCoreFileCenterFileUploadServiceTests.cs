using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心EfCoreFileCenterFileUploadServiceTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterFileUploadServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterStorageAppService _storageAppService;
    private readonly IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid> _blobObjectRepository;
    private readonly IBlobContainer<PrivateCloudDrive.FileCenter.FileCenterBlobContainer> _blobContainer;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterFileUploadServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterFileUploadServiceTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _storageAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterStorageAppService>();
        _blobObjectRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.BlobObject, Guid>>();
        _blobContainer = GetRequiredService<IBlobContainer<PrivateCloudDrive.FileCenter.FileCenterBlobContainer>>();
        _softDeleteFilter = GetRequiredService<IDataFilter<ISoftDelete>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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

    /// <summary>
    /// 验证 V1.1 容量统计按当前用户的 BlobObject 聚合文件大小。
    /// </summary>
    [Fact]
    public async Task Should_Report_Current_User_Storage_Usage()
    {
        var userId = Guid.NewGuid();
        var firstContent = Encoding.UTF8.GetBytes("first usage");
        var secondContent = Encoding.UTF8.GetBytes("second usage file");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using (var stream = new MemoryStream(firstContent))
            {
                await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    "usage-a.txt",
                    "text/plain",
                    stream,
                    firstContent.Length);
            }

            await using (var stream = new MemoryStream(secondContent))
            {
                await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    "usage-b.txt",
                    "text/plain",
                    stream,
                    secondContent.Length);
            }

            var usage = await _storageAppService.GetUsageAsync();

            usage.UsedBytes.ShouldBe(firstContent.Length + secondContent.Length);
            usage.IsQuotaConfigured.ShouldBeTrue();
            usage.QuotaBytes.ShouldBeGreaterThan(usage.UsedBytes);
            usage.RemainingBytes.ShouldBe(usage.QuotaBytes - usage.UsedBytes);
            usage.UsagePercent.ShouldBeGreaterThanOrEqualTo(0);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Delete_File_To_Recycle_Bin()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("deleted text");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "deleted.txt",
                contentType: "text/plain",
                stream,
                content.Length);

            await _fileUploadService.DeleteAsync(fileNode.Id);

            var activeList = await _foldersAppService.GetListAsync(
                new PrivateCloudDrive.FileCenter.GetFolderChildrenInput
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            activeList.Items.ShouldNotContain(item => item.Id == fileNode.Id);

            var deletedList = await _foldersAppService.GetDeletedListAsync(
                new Volo.Abp.Application.Dtos.PagedResultRequestDto
                {
                    SkipCount = 0,
                    MaxResultCount = 10
                });

            deletedList.Items.Single(item => item.Id == fileNode.Id).Name.ShouldBe("deleted.txt");
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Release_Blob_When_File_Is_Permanently_Deleted()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("permanently deleted text");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "purged.txt",
                contentType: "text/plain",
                stream,
                content.Length);

            var blobName = fileNode.BlobName!;
            (await _blobContainer.ExistsAsync(blobName)).ShouldBeTrue();

            await _fileUploadService.DeleteAsync(fileNode.Id);
            await _foldersAppService.PermanentDeleteAsync(fileNode.Id);

            (await _blobContainer.ExistsAsync(blobName)).ShouldBeFalse();

            var remainingBlobObjects = await WithUnitOfWorkAsync(async () =>
            {
                using (_softDeleteFilter.Disable())
                {
                    return await _blobObjectRepository.GetListAsync(blob => blob.BlobName == blobName);
                }
            });

            remainingBlobObjects.Count.ShouldBe(0);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reject_When_User_Storage_Quota_Is_Exceeded()
    {
        const long defaultQuota = 10L * 1024 * 1024 * 1024;
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("quota overflow");

        await WithCurrentUserAsync(userId, async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await _blobObjectRepository.InsertAsync(
                    PrivateCloudDrive.FileCenter.BlobObject.Create(
                        Guid.NewGuid(),
                        tenantId: null,
                        ownerId: userId,
                        blobName: "quota/full.bin",
                        fileName: "full.bin",
                        size: defaultQuota,
                        contentType: "application/octet-stream"),
                    autoSave: true);
            });

            await using var stream = new MemoryStream(content);

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _fileUploadService.UploadSmallFileAsync(
                    parentId: null,
                    fileName: "overflow.bin",
                    contentType: "application/octet-stream",
                    stream,
                    content.Length);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterStorageQuotaExceeded);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
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
