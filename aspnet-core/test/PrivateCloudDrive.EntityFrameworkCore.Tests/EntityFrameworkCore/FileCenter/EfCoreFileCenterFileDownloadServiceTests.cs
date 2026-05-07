using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterFileDownloadServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService _fileDownloadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public EfCoreFileCenterFileDownloadServiceTests()
    {
        _fileDownloadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService>();
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    [Fact]
    public async Task Should_Open_Download_Stream_For_File_Owner()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("downloadable content");

        await WithCurrentUserAsync(userId, async () =>
        {
            await using var uploadStream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "movie.mp4",
                contentType: "video/mp4",
                uploadStream,
                content.Length);

            var download = await _fileDownloadService.GetDownloadAsync(fileNode.Id);

            download.FileName.ShouldBe("movie.mp4");
            download.ContentType.ShouldBe("video/mp4");
            download.Size.ShouldBe(content.Length);

            await using (download.Content)
            {
                using var downloaded = new MemoryStream();
                await download.Content.CopyToAsync(downloaded);

                downloaded.ToArray().ShouldBe(content);
            }
        });
    }

    [Fact]
    public async Task Should_Reject_Download_From_Other_User()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("private content");
        Guid fileNodeId = default;

        await WithCurrentUserAsync(ownerId, async () =>
        {
            await using var stream = new MemoryStream(content);
            var fileNode = await _fileUploadService.UploadSmallFileAsync(
                parentId: null,
                fileName: "private.txt",
                contentType: "text/plain",
                stream,
                content.Length);

            fileNodeId = fileNode.Id;
        });

        await WithCurrentUserAsync(otherUserId, async () =>
        {
            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _fileDownloadService.GetDownloadAsync(fileNodeId);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
        });
    }

    [Fact]
    public async Task Should_Reject_Folder_Download()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var folder = await _foldersAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFolderInput { Name = "Folder" });

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _fileDownloadService.GetDownloadAsync(folder.Id);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyFileCanBeDownloaded);
        });
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "file-download-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
