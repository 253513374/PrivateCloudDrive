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

/// <summary>
/// 表示文件中心FileCenterFilesControllerTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class FileCenterFilesControllerTests
{
    /// <summary>
    /// 获取文件下载信息，并保留权限检查、范围下载和内容类型处理边界。
    /// </summary>
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

    /// <summary>
    /// 获取文件下载信息，并保留权限检查、范围下载和内容类型处理边界。
    /// </summary>
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

        /// <summary>
        /// 执行StubDownloadService操作，封装该场景下的业务规则、异常处理和结果返回。
        /// </summary>
        public StubDownloadService(string fileName, string contentType)
        {
            _fileName = fileName;
            _contentType = contentType;
        }

        /// <summary>
        /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
        /// </summary>
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

        /// <summary>
        /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
        /// </summary>
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
        /// <summary>
        /// 处理文件上传或保存请求，校验大小、归属和存储一致性后写入数据。
        /// </summary>
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

        /// <summary>
        /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
        /// </summary>
        public Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
