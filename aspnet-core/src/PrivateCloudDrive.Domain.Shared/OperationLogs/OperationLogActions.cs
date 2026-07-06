namespace PrivateCloudDrive.OperationLogs;

/// <summary>
/// 表示OperationLogActions组件，封装对应业务场景的状态或行为。
/// </summary>
public static class OperationLogActions
{
    public const string FileUpload = "FileUpload";
    public const string FileDownload = "FileDownload";
    public const string FileDelete = "FileDelete";
    public const string FileRestore = "FileRestore";
    public const string FilePermanentDelete = "FilePermanentDelete";
    public const string TrashEmpty = "TrashEmpty";
    public const string FolderCreate = "FolderCreate";
    public const string ShareCreate = "ShareCreate";
    public const string ShareDelete = "ShareDelete";
    public const string ShareAccess = "ShareAccess";
    public const string ShareDownload = "ShareDownload";
    public const string TagCreate = "TagCreate";
    public const string TagUpdate = "TagUpdate";
    public const string TagDelete = "TagDelete";
    public const string TagAddToFile = "TagAddToFile";
    public const string TagRemoveFromFile = "TagRemoveFromFile";
    public const string FavoriteSet = "FavoriteSet";

    /// <summary>
    /// 批量删除到回收站。
    /// </summary>
    public const string BatchDelete = "BatchDelete";

    /// <summary>
    /// 批量从回收站恢复。
    /// </summary>
    public const string BatchRestore = "BatchRestore";

    /// <summary>
    /// 批量永久删除。
    /// </summary>
    public const string BatchPermanentDelete = "BatchPermanentDelete";

    /// <summary>
    /// 批量移动。
    /// </summary>
    public const string BatchMove = "BatchMove";

    /// <summary>
    /// 批量设置收藏。
    /// </summary>
    public const string BatchFavoriteSet = "BatchFavoriteSet";

    public const string Security = "Security";
}
