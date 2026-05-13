using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aliyun.OSS;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aliyun;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 提供 FileCenter Blob 内容读取能力，封装完整读取和按字节范围读取的 Provider 差异。
/// </summary>
public interface IFileCenterBlobContentReader
{
    /// <summary>
    /// 打开指定 Blob 的完整读取流。
    /// </summary>
    Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开指定 Blob 的字节范围读取流。
    /// </summary>
    Task<Stream> OpenReadRangeAsync(
        string blobName,
        long start,
        long end,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 读取 FileCenter Blob 内容，并在阿里云 OSS Provider 下使用原生 Range 请求读取部分内容。
/// </summary>
public class FileCenterBlobContentReader : IFileCenterBlobContentReader, ITransientDependency
{
    private const int StreamBufferSize = 81920;

    private static readonly string ContainerName =
        BlobContainerNameAttribute.GetContainerName(typeof(FileCenterBlobContainer));

    private readonly IBlobContainer<FileCenterBlobContainer> _blobContainer;
    private readonly IBlobContainerConfigurationProvider _configurationProvider;
    private readonly IBlobNormalizeNamingService _blobNormalizeNamingService;
    private readonly IAliyunBlobNameCalculator _aliyunBlobNameCalculator;
    private readonly IOssClientFactory _ossClientFactory;

    /// <summary>
    /// 初始化 <see cref="FileCenterBlobContentReader"/> 的新实例。
    /// </summary>
    public FileCenterBlobContentReader(
        IBlobContainer<FileCenterBlobContainer> blobContainer,
        IBlobContainerConfigurationProvider configurationProvider,
        IBlobNormalizeNamingService blobNormalizeNamingService,
        IAliyunBlobNameCalculator aliyunBlobNameCalculator,
        IOssClientFactory ossClientFactory)
    {
        _blobContainer = blobContainer;
        _configurationProvider = configurationProvider;
        _blobNormalizeNamingService = blobNormalizeNamingService;
        _aliyunBlobNameCalculator = aliyunBlobNameCalculator;
        _ossClientFactory = ossClientFactory;
    }

    /// <summary>
    /// 打开指定 Blob 的完整读取流。
    /// </summary>
    public virtual Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        return _blobContainer.GetAsync(blobName, cancellationToken);
    }

    /// <summary>
    /// 打开指定 Blob 的字节范围读取流，OSS 使用服务端 Range，本地存储使用流定位或跳过。
    /// </summary>
    public virtual async Task<Stream> OpenReadRangeAsync(
        string blobName,
        long start,
        long end,
        CancellationToken cancellationToken = default)
    {
        if (start < 0 || end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        var configuration = _configurationProvider.Get(ContainerName);
        if (configuration.ProviderType == typeof(AliyunBlobProvider))
        {
            return await OpenAliyunRangeAsync(configuration, blobName, start, end, cancellationToken);
        }

        var stream = await _blobContainer.GetAsync(blobName, cancellationToken);
        if (stream.CanSeek)
        {
            stream.Position = start;
        }
        else
        {
            await SkipAsync(stream, start, cancellationToken);
        }

        return new FileCenterLimitedReadStream(stream, end - start + 1);
    }

    /// <summary>
    /// 通过阿里云 OSS SDK 原生 Range 请求打开对象内容流。
    /// </summary>
    private Task<Stream> OpenAliyunRangeAsync(
        BlobContainerConfiguration configuration,
        string blobName,
        long start,
        long end,
        CancellationToken cancellationToken)
    {
        var aliyunConfiguration = configuration.GetAliyunConfiguration();
        var normalizedNaming = _blobNormalizeNamingService.NormalizeNaming(configuration, ContainerName, blobName);
        var normalizedContainerName = normalizedNaming.ContainerName ?? ContainerName;
        var normalizedBlobName = normalizedNaming.BlobName ?? blobName;
        var objectName = _aliyunBlobNameCalculator.Calculate(
            new BlobProviderGetArgs(
                normalizedContainerName,
                configuration,
                normalizedBlobName,
                cancellationToken));

        var client = _ossClientFactory.Create(aliyunConfiguration);
        var request = new GetObjectRequest(aliyunConfiguration.ContainerName, objectName);
        request.SetRange(start, end);

        return Task.Run<Stream>(() => client.GetObject(request).Content, cancellationToken);
    }

    /// <summary>
    /// 在不可定位的流中跳过指定字节数。
    /// </summary>
    private static async Task SkipAsync(Stream stream, long bytesToSkip, CancellationToken cancellationToken)
    {
        var buffer = new byte[StreamBufferSize];
        var remaining = bytesToSkip;

        while (remaining > 0)
        {
            var readLength = (int)Math.Min(buffer.Length, remaining);
            var read = await stream.ReadAsync(buffer.AsMemory(0, readLength), cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException("The blob stream ended before the requested range start.");
            }

            remaining -= read;
        }
    }
}

/// <summary>
/// 包装底层读取流，限制最多读取指定字节数。
/// </summary>
internal sealed class FileCenterLimitedReadStream : Stream
{
    private readonly Stream _innerStream;
    private long _remaining;

    /// <summary>
    /// 初始化 <see cref="FileCenterLimitedReadStream"/> 的新实例。
    /// </summary>
    public FileCenterLimitedReadStream(Stream innerStream, long length)
    {
        _innerStream = innerStream;
        _remaining = length;
    }

    /// <summary>
    /// 获取当前流是否支持读取。
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// 获取当前流是否支持定位。
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// 获取当前流是否支持写入。
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// 当前包装流不公开总长度。
    /// </summary>
    public override long Length => throw new NotSupportedException();

    /// <summary>
    /// 当前包装流不支持读取或设置位置。
    /// </summary>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// 只读流无需刷新。
    /// </summary>
    public override void Flush()
    {
    }

    /// <summary>
    /// 同步读取最多剩余限制长度以内的数据。
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_remaining <= 0)
        {
            return 0;
        }

        var read = _innerStream.Read(buffer, offset, (int)Math.Min(count, _remaining));
        _remaining -= read;
        return read;
    }

    /// <summary>
    /// 异步读取最多剩余限制长度以内的数据。
    /// </summary>
    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (_remaining <= 0)
        {
            return 0;
        }

        var read = await _innerStream.ReadAsync(
            buffer[..(int)Math.Min(buffer.Length, _remaining)],
            cancellationToken);

        _remaining -= read;
        return read;
    }

    /// <summary>
    /// 当前包装流不支持定位。
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 当前包装流不支持设置长度。
    /// </summary>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 当前包装流不支持写入。
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 释放包装的底层流。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }

        base.Dispose(disposing);
    }
}
