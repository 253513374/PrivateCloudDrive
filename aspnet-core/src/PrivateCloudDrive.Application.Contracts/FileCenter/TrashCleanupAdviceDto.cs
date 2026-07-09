namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 回收站清理建议 DTO。
/// 包含空间占用统计、保留天数和建议文案，方便客户端向用户展示。
/// 不包含具体的文件名或路径。
/// </summary>
public class TrashCleanupAdviceDto
{
    /// <summary>
    /// 回收站当前占用空间（字节）。
    /// </summary>
    public long TrashSizeBytes { get; set; }

    /// <summary>
    /// 回收站中的文件数量（仅根节点计数，不含子节点）。
    /// </summary>
    public int TrashFileCount { get; set; }

    /// <summary>
    /// 回收站中的文件夹数量（仅根节点计数，不含子节点）。
    /// </summary>
    public int TrashFolderCount { get; set; }

    /// <summary>
    /// 回收站保留天数（默认 30 天）。
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// 即将被自动清理的根节点数量（超过保留天数）。
    /// </summary>
    public int AutoCleanupCount { get; set; }

    /// <summary>
    /// 即将被自动清理的总空间（字节）。
    /// </summary>
    public long AutoCleanupSizeBytes { get; set; }

    /// <summary>
    /// 清理建议可读文案。
    /// </summary>
    public string CleanupAdviceMessage { get; set; } = string.Empty;
}
