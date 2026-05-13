using System.IO;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示客户端通过 HTTP Range 请求指定的文件读取范围。
/// </summary>
public sealed class FileDownloadRangeRequest
{
    /// <summary>
    /// 获取或设置 Range 起始字节位置；为空且 End 有值时表示后缀长度请求。
    /// </summary>
    public long? Start { get; set; }

    /// <summary>
    /// 获取或设置 Range 结束字节位置；当 Start 为空时表示后缀长度。
    /// </summary>
    public long? End { get; set; }

    /// <summary>
    /// 根据文件总大小将请求范围转换为明确的起止字节范围。
    /// </summary>
    public FileDownloadRange Normalize(long totalSize)
    {
        if (totalSize <= 0)
        {
            throw new FileDownloadRangeNotSatisfiableException(totalSize);
        }

        long start;
        long end;

        if (Start.HasValue)
        {
            start = Start.Value;
            end = End ?? totalSize - 1;
        }
        else if (End.HasValue)
        {
            var suffixLength = End.Value;
            if (suffixLength <= 0)
            {
                throw new FileDownloadRangeNotSatisfiableException(totalSize);
            }

            start = suffixLength >= totalSize ? 0 : totalSize - suffixLength;
            end = totalSize - 1;
        }
        else
        {
            throw new FileDownloadRangeNotSatisfiableException(totalSize);
        }

        if (start < 0 || end < start || start >= totalSize)
        {
            throw new FileDownloadRangeNotSatisfiableException(totalSize);
        }

        return new FileDownloadRange
        {
            Start = start,
            End = long.Min(end, totalSize - 1),
            TotalSize = totalSize
        };
    }
}

/// <summary>
/// 表示已经按文件总大小校验并规范化后的文件读取范围。
/// </summary>
public sealed class FileDownloadRange
{
    /// <summary>
    /// 获取或设置读取范围的起始字节位置。
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// 获取或设置读取范围的结束字节位置。
    /// </summary>
    public long End { get; set; }

    /// <summary>
    /// 获取或设置完整文件大小。
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 获取当前范围包含的字节数。
    /// </summary>
    public long Length => End - Start + 1;
}

/// <summary>
/// 表示请求的文件读取范围无法被当前文件大小满足。
/// </summary>
public sealed class FileDownloadRangeNotSatisfiableException : IOException
{
    /// <summary>
    /// 初始化 <see cref="FileDownloadRangeNotSatisfiableException"/> 的新实例。
    /// </summary>
    public FileDownloadRangeNotSatisfiableException(long totalSize)
        : base($"The requested file range cannot be satisfied. Total size: {totalSize}.")
    {
        TotalSize = totalSize;
    }

    /// <summary>
    /// 获取完整文件大小，用于生成 Content-Range 响应头。
    /// </summary>
    public long TotalSize { get; }
}

/// <summary>
/// 表示 FileCenter 文件下载或预览所需的响应元数据和内容流。
/// </summary>
public class FileDownloadInfo
{
    /// <summary>
    /// 获取或设置返回给客户端的文件名。
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// 获取或设置响应内容类型。
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// 获取或设置本次响应内容长度。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 获取或设置完整文件大小。
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 获取或设置本次响应对应的文件读取范围；为空表示返回完整文件。
    /// </summary>
    public FileDownloadRange? Range { get; set; }

    /// <summary>
    /// 获取或设置文件内容流。
    /// </summary>
    public Stream Content { get; set; } = null!;
}
