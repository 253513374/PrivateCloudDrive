using System;
using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 创建分片上传会话的输入参数。
/// </summary>
public class CreateUploadSessionInput
{
    public Guid? ParentId { get; set; }

    [Required]
    [StringLength(UploadSessionConsts.MaxFileNameLength)]
    public string FileName { get; set; } = null!;

    [Range(1, long.MaxValue)]
    public long TotalSize { get; set; }

    [Range(1, int.MaxValue)]
    public int ChunkSize { get; set; } = 8 * 1024 * 1024;

    [Range(1, int.MaxValue)]
    public int TotalChunks { get; set; }

    [StringLength(UploadSessionConsts.MaxContentTypeLength)]
    public string? ContentType { get; set; }

    [StringLength(UploadSessionConsts.MaxSha256Length, MinimumLength = UploadSessionConsts.MaxSha256Length)]
    public string? Sha256 { get; set; }
}
