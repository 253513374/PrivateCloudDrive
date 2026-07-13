namespace PrivateCloudDrive.App;

/// <summary>
/// 表示AppShell组件，封装对应业务场景的状态或行为。
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// 初始化 <see cref="AppShell"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("file-details", typeof(Views.FileDetailsPage));
        Routing.RegisterRoute("media-preview", typeof(Views.MediaPreviewPage));
        Routing.RegisterRoute("media-albums", typeof(Views.MediaAlbumsPage));
        Routing.RegisterRoute("media-album-detail", typeof(Views.MediaAlbumDetailPage));
        Routing.RegisterRoute("media-album-add", typeof(Views.AddMediaToAlbumPage));
        Routing.RegisterRoute("media-processing", typeof(Views.MediaProcessingStatusPage));
        Routing.RegisterRoute("shares", typeof(Views.SharesPage));
        Routing.RegisterRoute("operation-logs", typeof(Views.OperationLogsPage));
        Routing.RegisterRoute("trash", typeof(Views.TrashPage));
        Routing.RegisterRoute("storage-usage", typeof(Views.StorageUsagePage));
        Routing.RegisterRoute("admin-users", typeof(Views.AdminUserManagementPage));
        Routing.RegisterRoute("admin-user-create", typeof(Views.AdminUserCreatePage));
        Routing.RegisterRoute("share-risk", typeof(Views.ShareRiskPage));
        Routing.RegisterRoute("storage-config", typeof(Views.StorageConfigPage));
    }
}
