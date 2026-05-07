using System;
using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

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
}
