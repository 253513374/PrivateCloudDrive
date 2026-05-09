using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 大文件分片上传会话聚合。
/// 记录分片大小、总分片数、已上传分片和过期时间，确保客户端可断点续传且服务端可校验分片边界。
/// </summary>
public class UploadSession : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid? ParentId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string NormalizedFileName { get; private set; } = null!;

    public long TotalSize { get; private set; }

    public int ChunkSize { get; private set; }

    public int TotalChunks { get; private set; }

    public string? ContentType { get; private set; }

    public string? Sha256 { get; private set; }

    public string UploadedChunksJson { get; private set; } = "[]";

    public UploadSessionStatus Status { get; private set; }

    public DateTime ExpirationTime { get; private set; }

    public Guid? FileNodeId { get; private set; }

    protected UploadSession()
    {
    }

    private UploadSession(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string fileName,
        long totalSize,
        int chunkSize,
        int totalChunks,
        DateTime expirationTime,
        string? contentType = null,
        string? sha256 = null)
        : base(id)
    {
        if (totalSize <= 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSession)
                .WithData("TotalSize", totalSize);
        }

        if (chunkSize <= 0 || totalChunks <= 0)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSession)
                .WithData("ChunkSize", chunkSize)
                .WithData("TotalChunks", totalChunks);
        }

        var expectedTotalChunks = (int)Math.Ceiling(totalSize / (double)chunkSize);
        if (expectedTotalChunks != totalChunks)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSession)
                .WithData("ExpectedTotalChunks", expectedTotalChunks)
                .WithData("TotalChunks", totalChunks);
        }

        TenantId = tenantId;
        OwnerId = ownerId;
        ParentId = parentId;
        TotalSize = totalSize;
        ChunkSize = chunkSize;
        TotalChunks = totalChunks;
        ExpirationTime = expirationTime;
        ContentType = Check.Length(contentType, nameof(contentType), UploadSessionConsts.MaxContentTypeLength);
        Sha256 = Check.Length(NormalizeSha256(sha256), nameof(sha256), UploadSessionConsts.MaxSha256Length);
        Status = UploadSessionStatus.Pending;

        SetFileName(fileName);
    }

    /// <summary>
    /// 创建待上传会话，并校验总大小、分片大小和总分片数是否匹配。
    /// </summary>
    public static UploadSession Create(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid? parentId,
        string fileName,
        long totalSize,
        int chunkSize,
        int totalChunks,
        DateTime expirationTime,
        string? contentType = null,
        string? sha256 = null)
    {
        return new UploadSession(
            id,
            tenantId,
            ownerId,
            parentId,
            fileName,
            totalSize,
            chunkSize,
            totalChunks,
            expirationTime,
            contentType,
            sha256);
    }

    /// <summary>
    /// 读取已成功上传的分片索引列表，用于客户端断点续传和进度展示。
    /// </summary>
    public IReadOnlyList<int> GetUploadedChunks()
    {
        return JsonSerializer.Deserialize<List<int>>(UploadedChunksJson) ?? new List<int>();
    }

    /// <summary>
    /// 标记指定分片已上传。重复上报同一分片不会重复记录。
    /// </summary>
    public void MarkChunkUploaded(int chunkIndex)
    {
        EnsurePending();
        EnsureValidChunkIndex(chunkIndex);

        var chunks = GetUploadedChunks().ToList();
        if (!chunks.Contains(chunkIndex))
        {
            chunks.Add(chunkIndex);
            chunks.Sort();
        }

        UploadedChunksJson = Check.Length(
            JsonSerializer.Serialize(chunks),
            nameof(UploadedChunksJson),
            UploadSessionConsts.MaxUploadedChunksJsonLength)!;
    }

    /// <summary>
    /// 计算指定分片期望大小；最后一个分片允许小于标准分片大小。
    /// </summary>
    public long GetExpectedChunkSize(int chunkIndex)
    {
        EnsureValidChunkIndex(chunkIndex);

        if (chunkIndex == TotalChunks - 1)
        {
            return TotalSize - (long)ChunkSize * chunkIndex;
        }

        return ChunkSize;
    }

    /// <summary>
    /// 上传会话完成后绑定最终生成的文件节点。
    /// </summary>
    public void Complete(Guid fileNodeId)
    {
        EnsurePending();

        Status = UploadSessionStatus.Completed;
        FileNodeId = fileNodeId;
    }

    /// <summary>
    /// 取消待上传会话，后续不允许继续上传分片或完成合并。
    /// </summary>
    public void Cancel()
    {
        EnsurePending();

        Status = UploadSessionStatus.Cancelled;
    }

    private void EnsurePending()
    {
        if (Status != UploadSessionStatus.Pending)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadSessionState)
                .WithData("Status", Status);
        }
    }

    private void EnsureValidChunkIndex(int chunkIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= TotalChunks)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterInvalidUploadChunkIndex)
                .WithData("ChunkIndex", chunkIndex)
                .WithData("TotalChunks", TotalChunks);
        }
    }

    private void SetFileName(string fileName)
    {
        var trimmedName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName)).Trim();

        FileName = Check.Length(
            trimmedName,
            nameof(fileName),
            UploadSessionConsts.MaxFileNameLength)!;

        NormalizedFileName = Check.Length(
            FileNode.NormalizeName(FileName),
            nameof(fileName),
            UploadSessionConsts.MaxNormalizedFileNameLength)!;
    }

    private static string? NormalizeSha256(string? sha256)
    {
        return string.IsNullOrWhiteSpace(sha256)
            ? null
            : sha256.Trim().ToLowerInvariant();
    }
}
