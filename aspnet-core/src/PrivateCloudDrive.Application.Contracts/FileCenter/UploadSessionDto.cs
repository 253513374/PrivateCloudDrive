using System;
using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 分片上传会话状态 DTO，供客户端展示进度和恢复上传。
/// </summary>
public class UploadSessionDto
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid OwnerId { get; set; }

    public Guid? ParentId { get; set; }

    public string FileName { get; set; } = null!;

    public long TotalSize { get; set; }

    public int ChunkSize { get; set; }

    public int TotalChunks { get; set; }

    public string? ContentType { get; set; }

    public string? Sha256 { get; set; }

    public UploadSessionStatus Status { get; set; }

    public DateTime ExpirationTime { get; set; }

    public Guid? FileNodeId { get; set; }

    public IReadOnlyList<int> UploadedChunks { get; set; } = new List<int>();

    public int UploadedChunkCount { get; set; }

    public long UploadedBytes { get; set; }

    public decimal ProgressPercent { get; set; }

    public bool IsRetryable { get; set; }

    public string StatusReason { get; set; } = "Unknown";

    public string? FailureReason { get; set; }

    public string NextAction { get; set; } = "StartNewUploadSession";
}
