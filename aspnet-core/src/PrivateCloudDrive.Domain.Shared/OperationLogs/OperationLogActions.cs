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
    public const string Security = "Security";
}
