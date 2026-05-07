using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.Controllers.FileCenter;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

public class FileCenterFilesControllerTests
{
    [Fact]
    public async Task Download_Should_Enable_Range_Processing()
    {
        var controller = CreateController("movie.mp4", "video/mp4");

        var result = await controller.DownloadAsync(Guid.NewGuid());
        var fileResult = result.ShouldBeOfType<FileStreamResult>();

        fileResult.EnableRangeProcessing.ShouldBeTrue();
        fileResult.FileDownloadName.ShouldBe("movie.mp4");
        fileResult.ContentType.ShouldBe("video/mp4");
    }

    [Fact]
    public async Task Content_Should_Enable_Range_Processing_Without_Attachment_Name()
    {
        var controller = CreateController("movie.mp4", "video/mp4");

        var result = await controller.ContentAsync(Guid.NewGuid());
        var fileResult = result.ShouldBeOfType<FileStreamResult>();

        fileResult.EnableRangeProcessing.ShouldBeTrue();
        fileResult.FileDownloadName.ShouldBeEmpty();
        fileResult.ContentType.ShouldBe("video/mp4");
    }

    private static FileCenterFilesController CreateController(string fileName, string contentType)
    {
        var controller = new FileCenterFilesController(
            new StubUploadService(),
            new StubDownloadService(fileName, contentType));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private class StubDownloadService : PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService
    {
        private readonly string _fileName;
        private readonly string _contentType;

        public StubDownloadService(string fileName, string contentType)
        {
            _fileName = fileName;
            _contentType = contentType;
        }

        public Task<PrivateCloudDrive.FileCenter.FileDownloadInfo> GetDownloadAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new PrivateCloudDrive.FileCenter.FileDownloadInfo
                {
                    FileName = _fileName,
                    ContentType = _contentType,
                    Size = 3,
                    Content = new MemoryStream(new byte[] { 1, 2, 3 })
                });
        }

        public Task<PrivateCloudDrive.FileCenter.FileDownloadInfo> GetThumbnailAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new PrivateCloudDrive.FileCenter.FileDownloadInfo
                {
                    FileName = _fileName,
                    ContentType = _contentType,
                    Size = 3,
                    Content = new MemoryStream(new byte[] { 1, 2, 3 })
                });
        }
    }

    private class StubUploadService : PrivateCloudDrive.FileCenter.IFileCenterFileUploadService
    {
        public Task<PrivateCloudDrive.FileCenter.FileNodeDto> UploadSmallFileAsync(
            Guid? parentId,
            string fileName,
            string? contentType,
            Stream stream,
            long size,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
