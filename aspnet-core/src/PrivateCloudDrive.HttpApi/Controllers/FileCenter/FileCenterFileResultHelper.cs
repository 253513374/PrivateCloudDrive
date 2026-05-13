using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using PrivateCloudDrive.FileCenter;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 提供 FileCenter 控制器复用的文件响应和 HTTP Range 处理逻辑。
/// </summary>
internal static class FileCenterFileResultHelper
{
    /// <summary>
    /// 从当前 HTTP 请求中解析单段 bytes Range 请求。
    /// </summary>
    public static bool TryCreateRangeRequest(
        HttpRequest request,
        out FileDownloadRangeRequest? range,
        out IActionResult? errorResult)
    {
        range = null;
        errorResult = null;

        var rangeHeaderValue = request.Headers[HeaderNames.Range].ToString();
        if (string.IsNullOrWhiteSpace(rangeHeaderValue))
        {
            return true;
        }

        if (!RangeHeaderValue.TryParse(rangeHeaderValue, out var parsedRange) ||
            !string.Equals(parsedRange.Unit.Value, "bytes", StringComparison.OrdinalIgnoreCase) ||
            parsedRange.Ranges.Count != 1)
        {
            errorResult = new StatusCodeResult(StatusCodes.Status416RangeNotSatisfiable);
            return false;
        }

        var parsedRangeItem = parsedRange.Ranges.Single();
        range = new FileDownloadRangeRequest
        {
            Start = parsedRangeItem.From,
            End = parsedRangeItem.To
        };

        return true;
    }

    /// <summary>
    /// 根据文件下载信息创建完整文件或部分内容的文件响应。
    /// </summary>
    public static IActionResult CreateFileResult(
        HttpContext httpContext,
        FileDownloadInfo file,
        bool asAttachment)
    {
        httpContext.Response.Headers[HeaderNames.AcceptRanges] = "bytes";

        var result = new FileStreamResult(file.Content, file.ContentType);
        if (asAttachment)
        {
            result.FileDownloadName = file.FileName;
        }

        if (file.Range == null)
        {
            result.EnableRangeProcessing = true;
            return result;
        }

        httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
        httpContext.Response.ContentLength = file.Range.Length;
        httpContext.Response.Headers[HeaderNames.ContentRange] =
            $"bytes {file.Range.Start}-{file.Range.End}/{file.Range.TotalSize}";

        return result;
    }

    /// <summary>
    /// 创建 Range 不可满足时的 416 响应。
    /// </summary>
    public static IActionResult CreateRangeNotSatisfiableResult(
        HttpContext httpContext,
        FileDownloadRangeNotSatisfiableException exception)
    {
        httpContext.Response.Headers[HeaderNames.ContentRange] = $"bytes */{exception.TotalSize}";
        return new StatusCodeResult(StatusCodes.Status416RangeNotSatisfiable);
    }
}
