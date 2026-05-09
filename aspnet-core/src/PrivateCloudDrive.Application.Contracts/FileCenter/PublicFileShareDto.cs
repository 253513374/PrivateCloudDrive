using System;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 匿名访问分享时返回的公开分享 DTO，不包含密码哈希、盐或底层 Blob 路径。
/// </summary>
public class PublicFileShareDto
{
    public string Token { get; set; } = string.Empty;

    public Guid FileNodeId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public FileNodeType NodeType { get; set; }

    public long Size { get; set; }

    public string? ContentType { get; set; }

    public DateTime? ExpirationTime { get; set; }

    public bool AllowDownload { get; set; }

    public bool PasswordRequired { get; set; }

    public int VisitCount { get; set; }
}
