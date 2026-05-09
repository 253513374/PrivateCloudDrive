using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using PrivateCloudDrive.Settings;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 大文件分片上传应用服务。
/// 负责创建上传会话、校验分片大小、落盘临时分片、合并文件、校验 SHA-256，并最终生成 FileNode 与媒体处理任务。
/// </summary>
public class FileCenterChunkUploadService : IFileCenterChunkUploadService, ITransientDependency
{
    private const long DefaultMaxUploadFileSizeInBytes = 104857600;
    private const long DefaultUserStorageQuotaInBytes = 10737418240;
    private const int SessionExpirationDays = 1;
    private const int StreamBufferSize = 81920;

    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly ISettingProvider _settingProvider;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IFileCenterBlobStoragePathProvider _storagePathProvider;
    private readonly IFileCenterBlobStorageService _blobStorageService;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IRepository<UploadSession, Guid> _uploadSessionRepository;
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly FileNodeManager _fileNodeManager;
    private readonly IFileCenterMediaAssetService _mediaAssetService;

    /// <summary>
    /// 初始化 <see cref="FileCenterChunkUploadService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterChunkUploadService(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ISettingProvider settingProvider,
        IAsyncQueryableExecuter asyncExecuter,
        IGuidGenerator guidGenerator,
        IClock clock,
        IFileCenterBlobStoragePathProvider storagePathProvider,
        IFileCenterBlobStorageService blobStorageService,
        IRepository<BlobObject, Guid> blobObjectRepository,
        IRepository<UploadSession, Guid> uploadSessionRepository,
        IFileNodeRepository fileNodeRepository,
        FileNodeManager fileNodeManager,
        IFileCenterMediaAssetService mediaAssetService)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _settingProvider = settingProvider;
        _asyncExecuter = asyncExecuter;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _storagePathProvider = storagePathProvider;
        _blobStorageService = blobStorageService;
        _blobObjectRepository = blobObjectRepository;
        _uploadSessionRepository = uploadSessionRepository;
        _fileNodeRepository = fileNodeRepository;
        _fileNodeManager = fileNodeManager;
        _mediaAssetService = mediaAssetService;
    }

    /// <summary>
    /// 创建分片上传会话；会先校验用户登录、文件大小、存储配额、目标目录和同名文件冲突。
    /// </summary>
    [UnitOfWork]
    public virtual async Task<UploadSessionDto> CreateAsync(CreateUploadSessionInput input)
    {
        var ownerId = GetOwnerId();
        var safeFileName = NormalizeFileName(input.FileName);
        var sha256 = NormalizeSha256(input.Sha256);

        await EnsureUploadSizeAsync(ownerId, input.TotalSize);
        await _fileNodeManager.EnsureCanCreateAsync(_currentTenant.Id, ownerId, input.ParentId, safeFileName);

        var session = UploadSession.Create(
            _guidGenerator.Create(),
            _currentTenant.Id,
            ownerId,
            input.ParentId,
            safeFileName,
            input.TotalSize,
            input.ChunkSize,
            input.TotalChunks,
            _clock.Now.AddDays(SessionExpirationDays),
            input.ContentType,
            sha256);

        await _uploadSessionRepository.InsertAsync(session, autoSave: true);

        return ToDto(session);
    }

    /// <summary>
    /// 获取当前用户拥有的上传会话，用于客户端恢复上传进度。
    /// </summary>
    public virtual async Task<UploadSessionDto> GetAsync(Guid id)
    {
        return ToDto(await GetOwnerSessionAsync(id));
    }

    /// <summary>
    /// 上传单个分片。服务端按会话记录校验分片索引和大小，防止客户端越界写入或错传分片。
    /// </summary>
    [UnitOfWork]
    public virtual async Task<UploadChunkResultDto> UploadChunkAsync(
        Guid id,
        int chunkIndex,
        Stream stream,
        long size,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOwnerSessionAsync(id);
        EnsurePendingSession(session);

        var expectedSize = session.GetExpectedChunkSize(chunkIndex);
        if (size != expectedSize)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadChunkSizeMismatch)
                .WithData("ChunkIndex", chunkIndex)
                .WithData("Size", size)
                .WithData("ExpectedSize", expectedSize);
        }

        var sessionDirectory = GetSessionDirectory(session);
        Directory.CreateDirectory(sessionDirectory);

        var chunkPath = GetChunkPath(sessionDirectory, chunkIndex);
        await SaveChunkAsync(stream, chunkPath, expectedSize, cancellationToken);

        session.MarkChunkUploaded(chunkIndex);
        await _uploadSessionRepository.UpdateAsync(session, autoSave: true, cancellationToken);

        return new UploadChunkResultDto
        {
            UploadedChunks = session.GetUploadedChunks()
        };
    }

    /// <summary>
    /// 完成上传会话：确认所有分片已上传，合并临时文件，校验总大小和 SHA-256，保存 Blob 并创建文件节点。
    /// </summary>
    [UnitOfWork]
    public virtual async Task<FileNodeDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnerSessionAsync(id);
        EnsurePendingSession(session);
        EnsureAllChunksUploaded(session);

        var sessionDirectory = GetSessionDirectory(session);
        var mergedPath = Path.Combine(sessionDirectory, "merged.upload");
        var mergeResult = await MergeChunksAsync(session, sessionDirectory, mergedPath, cancellationToken);

        if (mergeResult.Size != session.TotalSize)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionIncomplete)
                .WithData("Size", mergeResult.Size)
                .WithData("TotalSize", session.TotalSize);
        }

        if (!session.Sha256.IsNullOrWhiteSpace() &&
            !string.Equals(session.Sha256, mergeResult.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionHashMismatch)
                .WithData("ExpectedSha256", session.Sha256)
                .WithData("ActualSha256", mergeResult.Sha256);
        }

        var ownerId = GetOwnerId();
        await _fileNodeManager.EnsureCanCreateAsync(_currentTenant.Id, ownerId, session.ParentId, session.FileName);

        BlobObject blobObject;
        await using (var mergedStream = new FileStream(
                         mergedPath,
                         FileMode.Open,
                         FileAccess.Read,
                         System.IO.FileShare.Read,
                         StreamBufferSize,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            blobObject = await _blobStorageService.SaveAsync(
                ownerId,
                session.FileName,
                session.ContentType,
                mergedStream,
                session.TotalSize,
                mergeResult.Sha256,
                cancellationToken);
        }

        var fileNode = await _fileNodeManager.CreateFileAsync(
            _currentTenant.Id,
            ownerId,
            session.ParentId,
            session.FileName,
            session.TotalSize,
            session.ContentType,
            blobObject.BlobName);

        await _fileNodeRepository.InsertAsync(fileNode, autoSave: true, cancellationToken);
        await _mediaAssetService.CreatePendingAssetAsync(fileNode);

        session.Complete(fileNode.Id);
        await _uploadSessionRepository.UpdateAsync(session, autoSave: true, cancellationToken);

        DeleteSessionDirectory(sessionDirectory);

        return ToFileNodeDto(fileNode);
    }

    /// <summary>
    /// 取消上传会话并清理本地临时分片目录。
    /// </summary>
    [UnitOfWork]
    public virtual async Task CancelAsync(Guid id)
    {
        var session = await GetOwnerSessionAsync(id);
        EnsurePendingSession(session);

        session.Cancel();
        await _uploadSessionRepository.UpdateAsync(session, autoSave: true);

        DeleteSessionDirectory(GetSessionDirectory(session));
    }

    private async Task<UploadSession> GetOwnerSessionAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var session = await _uploadSessionRepository.FindAsync(id);

        if (session == null || session.TenantId != _currentTenant.Id || session.OwnerId != ownerId)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionNotFound)
                .WithData("Id", id);
        }

        return session;
    }

    private void EnsurePendingSession(UploadSession session)
    {
        if (session.Status != UploadSessionStatus.Pending)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSessionState)
                .WithData("Status", session.Status);
        }

        if (session.ExpirationTime < _clock.Now)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSessionState)
                .WithData("ExpirationTime", session.ExpirationTime);
        }
    }

    private static void EnsureAllChunksUploaded(UploadSession session)
    {
        var uploadedChunks = session.GetUploadedChunks();
        if (uploadedChunks.Count != session.TotalChunks)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionIncomplete)
                .WithData("UploadedChunks", uploadedChunks.Count)
                .WithData("TotalChunks", session.TotalChunks);
        }

        for (var i = 0; i < session.TotalChunks; i++)
        {
            if (!uploadedChunks.Contains(i))
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionIncomplete)
                    .WithData("MissingChunkIndex", i);
            }
        }
    }

    private async Task EnsureUploadSizeAsync(Guid ownerId, long size)
    {
        if (size <= 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSession)
                .WithData("Size", size);
        }

        var maxUploadFileSize = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.MaxUploadFileSizeInBytes,
            DefaultMaxUploadFileSizeInBytes);

        if (size > maxUploadFileSize)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterFileTooLarge)
                .WithData("Size", size)
                .WithData("MaxSize", maxUploadFileSize);
        }

        var quota = await GetLongSettingAsync(
            PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes,
            DefaultUserStorageQuotaInBytes);

        var usedStorageSize = await GetUsedStorageSizeAsync(ownerId);

        if (usedStorageSize + size > quota)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterStorageQuotaExceeded)
                .WithData("Size", size)
                .WithData("UsedSize", usedStorageSize)
                .WithData("Quota", quota);
        }
    }

    private async Task<long> GetUsedStorageSizeAsync(Guid ownerId)
    {
        var queryable = await _blobObjectRepository.GetQueryableAsync();
        var sizes = await _asyncExecuter.ToListAsync(
            queryable
                .Where(blob => blob.TenantId == _currentTenant.Id && blob.OwnerId == ownerId)
                .Select(blob => blob.Size));

        return sizes.Sum();
    }

    private async Task<long> GetLongSettingAsync(string name, long defaultValue)
    {
        var value = await _settingProvider.GetOrNullAsync(name);

        return long.TryParse(value, out var parsedValue)
            ? parsedValue
            : defaultValue;
    }

    private string GetSessionDirectory(UploadSession session)
    {
        var tenantPart = session.TenantId?.ToString("N") ?? "host";

        return Path.GetFullPath(
            Path.Combine(
                _storagePathProvider.GetStorageRootPath(),
                "temp",
                "uploads",
                tenantPart,
                session.OwnerId.ToString("N"),
                session.Id.ToString("N")));
    }

    private static string GetChunkPath(string sessionDirectory, int chunkIndex)
    {
        return Path.Combine(sessionDirectory, $"{chunkIndex:D10}.chunk");
    }

    /// <summary>
    /// 保存单个分片到会话临时目录，并确保实际写入字节数与期望大小一致。
    /// </summary>
    private static async Task SaveChunkAsync(
        Stream stream,
        string chunkPath,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var fileStream = new FileStream(
            chunkPath,
            FileMode.Create,
            FileAccess.Write,
            System.IO.FileShare.None,
            StreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await stream.CopyToAsync(fileStream, cancellationToken);

        if (fileStream.Length != expectedSize)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadChunkSizeMismatch)
                .WithData("Size", fileStream.Length)
                .WithData("ExpectedSize", expectedSize);
        }
    }

    /// <summary>
    /// 按分片索引顺序合并临时文件，同时计算最终文件大小和 SHA-256。
    /// </summary>
    private static async Task<(long Size, string Sha256)> MergeChunksAsync(
        UploadSession session,
        string sessionDirectory,
        string mergedPath,
        CancellationToken cancellationToken)
    {
        await using var mergedStream = new FileStream(
            mergedPath,
            FileMode.Create,
            FileAccess.Write,
            System.IO.FileShare.None,
            StreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var sha256 = SHA256.Create();
        var buffer = new byte[StreamBufferSize];

        for (var chunkIndex = 0; chunkIndex < session.TotalChunks; chunkIndex++)
        {
            var chunkPath = GetChunkPath(sessionDirectory, chunkIndex);
            if (!File.Exists(chunkPath))
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionIncomplete)
                    .WithData("MissingChunkIndex", chunkIndex);
            }

            await using var chunkStream = new FileStream(
                chunkPath,
                FileMode.Open,
                FileAccess.Read,
                System.IO.FileShare.Read,
                StreamBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            long chunkSize = 0;
            int bytesRead;
            while ((bytesRead = await chunkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await mergedStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                chunkSize += bytesRead;
            }

            var expectedChunkSize = session.GetExpectedChunkSize(chunkIndex);
            if (chunkSize != expectedChunkSize)
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterUploadChunkSizeMismatch)
                    .WithData("ChunkIndex", chunkIndex)
                    .WithData("Size", chunkSize)
                    .WithData("ExpectedSize", expectedChunkSize);
            }
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return (mergedStream.Length, Convert.ToHexString(sha256.Hash!).ToLowerInvariant());
    }

    private static void DeleteSessionDirectory(string sessionDirectory)
    {
        if (Directory.Exists(sessionDirectory))
        {
            Directory.Delete(sessionDirectory, recursive: true);
        }
    }

    private Guid GetOwnerId()
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return _currentUser.Id.Value;
    }

    private static string NormalizeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeFileName) ||
            safeFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidFileName)
                .WithData("FileName", fileName);
        }

        return safeFileName;
    }

    private static string? NormalizeSha256(string? sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
        {
            return null;
        }

        var normalizedSha256 = sha256.Trim().ToLowerInvariant();
        if (normalizedSha256.Length != UploadSessionConsts.MaxSha256Length ||
            normalizedSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSession)
                .WithData("Sha256", sha256);
        }

        return normalizedSha256;
    }

    private static UploadSessionDto ToDto(UploadSession session)
    {
        return new UploadSessionDto
        {
            Id = session.Id,
            TenantId = session.TenantId,
            OwnerId = session.OwnerId,
            ParentId = session.ParentId,
            FileName = session.FileName,
            TotalSize = session.TotalSize,
            ChunkSize = session.ChunkSize,
            TotalChunks = session.TotalChunks,
            ContentType = session.ContentType,
            Sha256 = session.Sha256,
            Status = session.Status,
            ExpirationTime = session.ExpirationTime,
            FileNodeId = session.FileNodeId,
            UploadedChunks = session.GetUploadedChunks()
        };
    }

    private static FileNodeDto ToFileNodeDto(FileNode node)
    {
        return new FileNodeDto
        {
            Id = node.Id,
            TenantId = node.TenantId,
            OwnerId = node.OwnerId,
            ParentId = node.ParentId,
            NodeType = node.NodeType,
            Name = node.Name,
            NormalizedName = node.NormalizedName,
            Size = node.Size,
            ContentType = node.ContentType,
            BlobName = node.BlobName,
            IsFavorite = node.IsFavorite,
            CreationTime = node.CreationTime,
            LastModificationTime = node.LastModificationTime
        };
    }
}
