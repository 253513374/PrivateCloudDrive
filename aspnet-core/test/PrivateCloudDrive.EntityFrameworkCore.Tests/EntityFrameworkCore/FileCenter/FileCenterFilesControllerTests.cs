using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using PrivateCloudDrive.Controllers.FileCenter;
using Shouldly;
using Volo.Abp;
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

    [Fact]
    public async Task Content_Should_Return_Partial_Content_For_Explicit_Range()
    {
        var controller = CreateController("movie.mp4", "video/mp4");
        controller.Request.Headers[HeaderNames.Range] = "bytes=1-2";

        var result = await controller.ContentAsync(Guid.NewGuid());
        var fileResult = result.ShouldBeOfType<FileStreamResult>();

        controller.Response.StatusCode.ShouldBe(StatusCodes.Status206PartialContent);
        controller.Response.Headers[HeaderNames.ContentRange].ToString().ShouldBe("bytes 1-2/3");
        controller.Response.ContentLength.ShouldBe(2);
        fileResult.EnableRangeProcessing.ShouldBeFalse();
        fileResult.FileDownloadName.ShouldBeEmpty();
    }

    [Fact]
    public async Task Content_Should_Reject_Invalid_Range()
    {
        var controller = CreateController("movie.mp4", "video/mp4");
        controller.Request.Headers[HeaderNames.Range] = "bytes=9-10";

        var result = await controller.ContentAsync(Guid.NewGuid());

        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status416RangeNotSatisfiable);
        controller.Response.Headers[HeaderNames.ContentRange].ToString().ShouldBe("bytes */3");
    }

    [Fact]
    public async Task Content_Should_Return_NotFound_When_File_Is_Not_Owned_By_Current_User()
    {
        var controller = CreateController(
            "movie.mp4",
            "video/mp4",
            downloadException: CreateNodeNotFoundException());

        var result = await controller.ContentAsync(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Download_Should_Return_NotFound_When_File_Is_Not_Owned_By_Current_User()
    {
        var controller = CreateController(
            "movie.mp4",
            "video/mp4",
            downloadException: CreateNodeNotFoundException());

        var result = await controller.DownloadAsync(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_NotFound_When_File_Is_Not_Owned_By_Current_User()
    {
        var controller = CreateController(
            "movie.mp4",
            "video/mp4",
            deleteException: CreateNodeNotFoundException());

        var result = await controller.DeleteAsync(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    private static FileCenterFilesController CreateController(
        string fileName,
        string contentType,
        BusinessException? downloadException = null,
        BusinessException? deleteException = null)
    {
        var controller = new FileCenterFilesController(
            new StubUploadService(deleteException),
            new StubDownloadService(fileName, contentType, downloadException));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static BusinessException CreateNodeNotFoundException()
    {
        return new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound);
    }

    private class StubDownloadService : PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService
    {
        private readonly string _fileName;
        private readonly string _contentType;
        private readonly BusinessException? _exception;

        /// <summary>
        /// 执行StubDownloadService操作，封装该场景下的业务规则、异常处理和结果返回。
        /// </summary>
        public StubDownloadService(
            string fileName,
            string contentType,
            BusinessException? exception = null)
        {
            _fileName = fileName;
            _contentType = contentType;
            _exception = exception;
        }

        /// <summary>
        /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
        /// </summary>
        public Task<PrivateCloudDrive.FileCenter.FileDownloadInfo> GetDownloadAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return GetDownloadAsync(id, range: null, cancellationToken);
        }

        public Task<PrivateCloudDrive.FileCenter.FileDownloadInfo> GetDownloadAsync(
            Guid id,
            PrivateCloudDrive.FileCenter.FileDownloadRangeRequest? range,
            CancellationToken cancellationToken = default)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            var normalizedRange = range?.Normalize(3);

            return Task.FromResult(
                new PrivateCloudDrive.FileCenter.FileDownloadInfo
                {
                    FileName = _fileName,
                    ContentType = _contentType,
                    Size = normalizedRange?.Length ?? 3,
                    TotalSize = 3,
                    Range = normalizedRange,
                    Content = normalizedRange == null
                        ? new MemoryStream(new byte[] { 1, 2, 3 })
                        : new MemoryStream(new byte[] { 2, 3 })
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
                    TotalSize = 3,
                    Content = new MemoryStream(new byte[] { 1, 2, 3 })
                });
        }
    }

    private class StubUploadService : PrivateCloudDrive.FileCenter.IFileCenterFileUploadService
    {
        private readonly BusinessException? _deleteException;

        public StubUploadService(BusinessException? deleteException = null)
        {
            _deleteException = deleteException;
        }

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
            if (_deleteException != null)
            {
                throw _deleteException;
            }

            throw new NotSupportedException();
        }
    }
}
