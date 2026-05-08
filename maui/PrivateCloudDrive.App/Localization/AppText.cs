using System.Globalization;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Localization;

public static class AppText
{
    public const string DefaultCultureName = "zh-CN";

    private static readonly IReadOnlyDictionary<string, TextPair> Texts = new Dictionary<string, TextPair>(StringComparer.Ordinal)
    {
        [nameof(Start)] = new("启动", "Start"),
        [nameof(SignIn)] = new("登录", "Sign In"),
        [nameof(SignInAction)] = new("登录", "Sign in"),
        [nameof(SigningIn)] = new("正在登录", "Signing in"),
        [nameof(CheckingSignInStatus)] = new("正在检查登录状态", "Checking sign-in status"),
        [nameof(RestoringSession)] = new("正在恢复会话", "Restoring session"),
        [nameof(StartupFailed)] = new("启动失败", "Startup failed"),
        [nameof(UnableToRestoreSignInState)] = new("无法恢复登录状态。{0}", "Unable to restore sign-in state. {0}"),
        [nameof(SignInWithWechat)] = new("使用微信登录", "Sign in with WeChat"),
        [nameof(UserNameOrEmail)] = new("用户名或邮箱", "Username or email"),
        [nameof(Password)] = new("密码", "Password"),
        [nameof(EnterUserNameAndPassword)] = new("请输入用户名和密码。", "Enter username and password."),
        [nameof(EnterUserNamePasswordThenWechat)] = new("请输入用户名和密码，然后再次使用微信完成绑定。", "Enter username and password, then use WeChat again to bind."),
        [nameof(WechatSignInCanceled)] = new("微信登录已取消。", "WeChat sign-in was canceled."),
        [nameof(WechatSignInFailed)] = new("微信登录失败。", "WeChat sign-in failed."),
        [nameof(Files)] = new("文件", "Files"),
        [nameof(Photos)] = new("图片", "Photos"),
        [nameof(Videos)] = new("视频", "Videos"),
        [nameof(Uploads)] = new("上传", "Uploads"),
        [nameof(Trash)] = new("回收站", "Trash"),
        [nameof(Settings)] = new("设置", "Settings"),
        [nameof(Details)] = new("详情", "Details"),
        [nameof(OperationLogs)] = new("操作日志", "Operation Logs"),
        [nameof(Refresh)] = new("刷新", "Refresh"),
        [nameof(Retry)] = new("重试", "Retry"),
        [nameof(Back)] = new("返回", "Back"),
        [nameof(More)] = new("更多", "More"),
        [nameof(Delete)] = new("删除", "Delete"),
        [nameof(Logout)] = new("退出登录", "Logout"),
        [nameof(SignOut)] = new("退出登录", "Sign out"),
        [nameof(Cancel)] = new("取消", "Cancel"),
        [nameof(Create)] = new("创建", "Create"),
        [nameof(Add)] = new("添加", "Add"),
        [nameof(Next)] = new("下一步", "Next"),
        [nameof(Move)] = new("移动", "Move"),
        [nameof(Empty)] = new("清空", "Empty"),
        [nameof(Restore)] = new("还原", "Restore"),
        [nameof(DeleteForever)] = new("永久删除", "Delete forever"),
        [nameof(NewFolder)] = new("新建文件夹", "New Folder"),
        [nameof(NewFolderLower)] = new("新建文件夹", "New folder"),
        [nameof(FolderName)] = new("文件夹名称", "Folder name"),
        [nameof(Upload)] = new("上传", "Upload"),
        [nameof(ThisFolderIsEmpty)] = new("此文件夹为空", "This folder is empty"),
        [nameof(EmptyFolderHelp)] = new("上传文件或创建文件夹来整理当前位置。", "Upload files or create a folder to start organizing this location."),
        [nameof(LoadingFiles)] = new("正在加载文件...", "Loading files..."),
        [nameof(LoadingPhotos)] = new("正在加载图片...", "Loading photos..."),
        [nameof(LoadingVideos)] = new("正在加载视频...", "Loading videos..."),
        [nameof(LoadingTrash)] = new("正在加载回收站...", "Loading trash..."),
        [nameof(LoadingOperationLogs)] = new("正在加载操作日志...", "Loading operation logs..."),
        [nameof(UnableToLoadFiles)] = new("无法加载文件。{0}", "Unable to load files. {0}"),
        [nameof(UnableToLoadPhotos)] = new("无法加载图片。{0}", "Unable to load photos. {0}"),
        [nameof(UnableToLoadVideos)] = new("无法加载视频。{0}", "Unable to load videos. {0}"),
        [nameof(UnableToLoadOperationLogs)] = new("无法加载操作日志。{0}", "Unable to load operation logs. {0}"),
        [nameof(SomeUploadsFailed)] = new("部分文件上传失败", "Some uploads failed"),
        [nameof(UploadFailed)] = new("上传失败", "Upload failed"),
        [nameof(UploadFailedBeforeRequest)] = new("{0}: 请求到达服务器前上传失败。", "{0}: upload failed before the request reached the server."),
        [nameof(SelectFilesToUpload)] = new("选择要上传的文件", "Select files to upload"),
        [nameof(AllFilesFilter)] = new("所有文件 (*.*)", "All files (*.*)"),
        [nameof(WindowsFileDialogFailed)] = new("Windows 文件选择器失败，错误码 0x{0}。", "Windows file dialog failed with error 0x{0}."),
        [nameof(UnableToCreateFolder)] = new("无法创建文件夹", "Unable to create folder"),
        [nameof(UnableToDelete)] = new("无法删除", "Unable to delete"),
        [nameof(MoveToTrash)] = new("移入回收站", "Move to trash"),
        [nameof(MoveToTrashQuestion)] = new("将 \"{0}\" 移入回收站？", "Move \"{0}\" to trash?"),
        [nameof(NoPhotosFound)] = new("暂无图片", "No photos found"),
        [nameof(NoPhotosHelp)] = new("从文件页上传图片后会显示在这里。", "Upload images from Files to populate this view."),
        [nameof(NoVideosFound)] = new("暂无视频", "No videos found"),
        [nameof(NoVideosHelp)] = new("从文件页上传视频后会显示在这里。", "Upload videos from Files to populate this view."),
        [nameof(PhotosCount)] = new("{0} 张图片", "{0} photos"),
        [nameof(VideosCount)] = new("{0} 个视频", "{0} videos"),
        [nameof(ActiveFailedCompletedUploads)] = new("活跃、失败和已完成的上传任务。", "Active, failed, and completed upload tasks."),
        [nameof(NoUploadTasks)] = new("暂无上传任务", "No upload tasks"),
        [nameof(NoUploadTasksHelp)] = new("从文件页开始上传。等待、上传中、失败和已完成的任务会显示在这里。", "Start uploads from Files. Waiting, uploading, failed, and completed tasks will appear here."),
        [nameof(ClearDone)] = new("清除已完成", "Clear Done"),
        [nameof(UploadQueueEmpty)] = new("上传队列为空。", "Upload queue is empty."),
        [nameof(UploadQueueSummary)] = new("{0} 个上传中，{1} 个等待，{2} 个失败，{3} 个已完成。", "{0} uploading, {1} waiting, {2} failed, {3} completed."),
        [nameof(Completed)] = new("已完成", "Completed"),
        [nameof(Waiting)] = new("等待中", "Waiting"),
        [nameof(Uploading)] = new("上传中", "Uploading"),
        [nameof(Failed)] = new("失败", "Failed"),
        [nameof(Unknown)] = new("未知", "Unknown"),
        [nameof(Type)] = new("类型", "Type"),
        [nameof(Size)] = new("大小", "Size"),
        [nameof(Modified)] = new("修改时间", "Modified"),
        [nameof(AvailableActions)] = new("可用操作", "Available actions"),
        [nameof(LoadingImagePreview)] = new("正在加载图片...", "Loading image..."),
        [nameof(UnableToLoadImagePreview)] = new("无法加载图片预览。{0}", "Unable to load image preview. {0}"),
        [nameof(Favorited)] = new("已收藏", "Favorited"),
        [nameof(NotFavorited)] = new("未收藏", "Not favorited"),
        [nameof(AddFavorite)] = new("添加收藏", "Add favorite"),
        [nameof(RemoveFavorite)] = new("取消收藏", "Remove favorite"),
        [nameof(AddTag)] = new("添加标签", "Add tag"),
        [nameof(TagName)] = new("标签名称", "Tag name"),
        [nameof(TagAdded)] = new("已添加标签：{0}", "Tag added: {0}"),
        [nameof(CreateShareLink)] = new("创建分享链接", "Create share link"),
        [nameof(ShareExpiration)] = new("分享有效期", "Share expiration"),
        [nameof(Days)] = new("天数", "Days"),
        [nameof(SharePassword)] = new("分享密码", "Share password"),
        [nameof(OptionalPassword)] = new("可选密码", "Optional password"),
        [nameof(ShareNotCreated)] = new("未创建分享", "Share not created"),
        [nameof(ExpirationDaysInvalid)] = new("有效天数必须为空或大于 0。", "Expiration days must be empty or greater than zero."),
        [nameof(ShareLinkCopied)] = new("分享链接已复制。", "Share link copied."),
        [nameof(ShareLinkCopiedPasswordRequired)] = new("分享链接已复制，需要密码访问。", "Share link copied. Password required."),
        [nameof(InvalidFileDetails)] = new("无法显示文件详情，因为所选项目 ID 无效。", "File details are unavailable because the selected item id is invalid."),
        [nameof(InvalidMediaId)] = new("媒体 ID 无效。", "Invalid media id."),
        [nameof(RestoreItemsOrRemovePermanently)] = new("还原项目或永久删除。", "Restore items or remove them permanently."),
        [nameof(TrashIsEmpty)] = new("回收站为空", "Trash is empty"),
        [nameof(TrashAlreadyEmpty)] = new("回收站已经为空。", "Trash is already empty."),
        [nameof(UnableToRestore)] = new("无法还原 \"{0}\"。{1} 如果原文件夹中已有同名项目，请先重命名或删除现有项目后再重试。", "Unable to restore \"{0}\". {1} If the original folder already has an item with the same name, rename or remove the active item before retrying."),
        [nameof(PermanentlyDeleteQuestion)] = new("永久删除 \"{0}\"？此操作无法撤销。", "Permanently delete \"{0}\"? This cannot be undone."),
        [nameof(UnableToPermanentlyDelete)] = new("无法永久删除 \"{0}\"。{1}", "Unable to permanently delete \"{0}\". {1}"),
        [nameof(EmptyTrash)] = new("清空回收站", "Empty trash"),
        [nameof(EmptyTrashQuestion)] = new("永久删除回收站中的全部项目？此操作无法撤销。", "Permanently delete all items in trash? This cannot be undone."),
        [nameof(UnableToEmptyTrash)] = new("无法清空回收站。{0}", "Unable to empty trash. {0}"),
        [nameof(AccountServerPreferences)] = new("账号、服务器和应用偏好。", "Account, server, and app preferences."),
        [nameof(Server)] = new("服务器", "Server"),
        [nameof(Security)] = new("安全", "Security"),
        [nameof(LocalSessionProtected)] = new("本地会话已在此设备上保护。退出登录可移除访问权限。", "Local session is protected on this device. Sign out to remove access."),
        [nameof(Wechat)] = new("微信", "WeChat"),
        [nameof(Checking)] = new("正在检查", "Checking"),
        [nameof(BindWechat)] = new("绑定微信", "Bind WeChat"),
        [nameof(Unbind)] = new("解绑", "Unbind"),
        [nameof(UnbindWechat)] = new("解绑微信", "Unbind WeChat"),
        [nameof(UnbindWechatQuestion)] = new("从此账号解绑微信？", "Unbind WeChat from this account?"),
        [nameof(CheckingLocalSession)] = new("正在检查本地会话", "Checking local session"),
        [nameof(SignedInOnThisDevice)] = new("已在此设备登录。", "Signed in on this device."),
        [nameof(NoValidLocalSession)] = new("没有有效的本地会话。请重新登录后访问私有文件。", "No valid local session. Sign in again to access private files."),
        [nameof(UnableToReadLocalSession)] = new("无法读取本地会话状态。{0}", "Unable to read local session state. {0}"),
        [nameof(Unavailable)] = new("不可用", "Unavailable"),
        [nameof(NotEnabled)] = new("未启用", "Not enabled"),
        [nameof(SignInRequired)] = new("需要先登录", "Sign in required"),
        [nameof(NotBound)] = new("未绑定", "Not bound"),
        [nameof(UnavailableOnThisDevice)] = new("此设备不可用", "Unavailable on this device"),
        [nameof(Bound)] = new("已绑定", "Bound"),
        [nameof(BoundWithName)] = new("已绑定：{0}", "Bound: {0}"),
        [nameof(WechatAuthorizationCanceled)] = new("微信授权已取消。", "WeChat authorization was canceled."),
        [nameof(SignOutQuestion)] = new("在此设备上退出 PrivateCloudDrive？", "Sign out of PrivateCloudDrive on this device?"),
        [nameof(NoLogsFound)] = new("暂无日志", "No logs found"),
        [nameof(LogsEmptyHelp)] = new("账号或文件活动后刷新查看。", "Refresh after account or file activity."),
        [nameof(UnknownUser)] = new("未知用户", "Unknown user"),
        [nameof(FileKindFolder)] = new("文件夹", "Folder"),
        [nameof(FileKindImage)] = new("图片", "Image"),
        [nameof(FileKindVideo)] = new("视频", "Video"),
        [nameof(FileKindArchive)] = new("压缩包", "Archive"),
        [nameof(FileKindDocument)] = new("文档", "Document"),
        [nameof(FileKindFile)] = new("文件", "File"),
    };

    public static string Start => Get(nameof(Start));
    public static string SignIn => Get(nameof(SignIn));
    public static string SignInAction => Get(nameof(SignInAction));
    public static string SigningIn => Get(nameof(SigningIn));
    public static string CheckingSignInStatus => Get(nameof(CheckingSignInStatus));
    public static string RestoringSession => Get(nameof(RestoringSession));
    public static string StartupFailed => Get(nameof(StartupFailed));
    public static string SignInWithWechat => Get(nameof(SignInWithWechat));
    public static string UserNameOrEmail => Get(nameof(UserNameOrEmail));
    public static string Password => Get(nameof(Password));
    public static string EnterUserNameAndPassword => Get(nameof(EnterUserNameAndPassword));
    public static string EnterUserNamePasswordThenWechat => Get(nameof(EnterUserNamePasswordThenWechat));
    public static string WechatSignInCanceled => Get(nameof(WechatSignInCanceled));
    public static string WechatSignInFailed => Get(nameof(WechatSignInFailed));
    public static string Files => Get(nameof(Files));
    public static string Photos => Get(nameof(Photos));
    public static string Videos => Get(nameof(Videos));
    public static string Uploads => Get(nameof(Uploads));
    public static string Trash => Get(nameof(Trash));
    public static string Settings => Get(nameof(Settings));
    public static string Details => Get(nameof(Details));
    public static string OperationLogs => Get(nameof(OperationLogs));
    public static string Refresh => Get(nameof(Refresh));
    public static string Retry => Get(nameof(Retry));
    public static string Back => Get(nameof(Back));
    public static string More => Get(nameof(More));
    public static string Delete => Get(nameof(Delete));
    public static string Logout => Get(nameof(Logout));
    public static string SignOut => Get(nameof(SignOut));
    public static string Cancel => Get(nameof(Cancel));
    public static string Create => Get(nameof(Create));
    public static string Add => Get(nameof(Add));
    public static string Next => Get(nameof(Next));
    public static string Move => Get(nameof(Move));
    public static string Empty => Get(nameof(Empty));
    public static string Restore => Get(nameof(Restore));
    public static string DeleteForever => Get(nameof(DeleteForever));
    public static string NewFolder => Get(nameof(NewFolder));
    public static string NewFolderLower => Get(nameof(NewFolderLower));
    public static string FolderName => Get(nameof(FolderName));
    public static string Upload => Get(nameof(Upload));
    public static string ThisFolderIsEmpty => Get(nameof(ThisFolderIsEmpty));
    public static string EmptyFolderHelp => Get(nameof(EmptyFolderHelp));
    public static string LoadingFiles => Get(nameof(LoadingFiles));
    public static string LoadingPhotos => Get(nameof(LoadingPhotos));
    public static string LoadingVideos => Get(nameof(LoadingVideos));
    public static string LoadingTrash => Get(nameof(LoadingTrash));
    public static string LoadingOperationLogs => Get(nameof(LoadingOperationLogs));
    public static string UnableToLoadFiles => Get(nameof(UnableToLoadFiles));
    public static string UnableToLoadPhotos => Get(nameof(UnableToLoadPhotos));
    public static string UnableToLoadVideos => Get(nameof(UnableToLoadVideos));
    public static string UnableToLoadOperationLogs => Get(nameof(UnableToLoadOperationLogs));
    public static string SomeUploadsFailed => Get(nameof(SomeUploadsFailed));
    public static string UploadFailed => Get(nameof(UploadFailed));
    public static string UploadFailedBeforeRequest => Get(nameof(UploadFailedBeforeRequest));
    public static string SelectFilesToUpload => Get(nameof(SelectFilesToUpload));
    public static string AllFilesFilter => Get(nameof(AllFilesFilter));
    public static string WindowsFileDialogFailed => Get(nameof(WindowsFileDialogFailed));
    public static string UnableToCreateFolder => Get(nameof(UnableToCreateFolder));
    public static string UnableToDelete => Get(nameof(UnableToDelete));
    public static string MoveToTrash => Get(nameof(MoveToTrash));
    public static string MoveToTrashQuestion => Get(nameof(MoveToTrashQuestion));
    public static string NoPhotosFound => Get(nameof(NoPhotosFound));
    public static string NoPhotosHelp => Get(nameof(NoPhotosHelp));
    public static string NoVideosFound => Get(nameof(NoVideosFound));
    public static string NoVideosHelp => Get(nameof(NoVideosHelp));
    public static string PhotosCount => Get(nameof(PhotosCount));
    public static string VideosCount => Get(nameof(VideosCount));
    public static string ActiveFailedCompletedUploads => Get(nameof(ActiveFailedCompletedUploads));
    public static string NoUploadTasks => Get(nameof(NoUploadTasks));
    public static string NoUploadTasksHelp => Get(nameof(NoUploadTasksHelp));
    public static string ClearDone => Get(nameof(ClearDone));
    public static string UploadQueueEmpty => Get(nameof(UploadQueueEmpty));
    public static string UploadQueueSummary => Get(nameof(UploadQueueSummary));
    public static string Completed => Get(nameof(Completed));
    public static string Waiting => Get(nameof(Waiting));
    public static string Uploading => Get(nameof(Uploading));
    public static string Failed => Get(nameof(Failed));
    public static string Unknown => Get(nameof(Unknown));
    public static string Type => Get(nameof(Type));
    public static string Size => Get(nameof(Size));
    public static string Modified => Get(nameof(Modified));
    public static string AvailableActions => Get(nameof(AvailableActions));
    public static string LoadingImagePreview => Get(nameof(LoadingImagePreview));
    public static string UnableToLoadImagePreview => Get(nameof(UnableToLoadImagePreview));
    public static string Favorited => Get(nameof(Favorited));
    public static string NotFavorited => Get(nameof(NotFavorited));
    public static string AddFavorite => Get(nameof(AddFavorite));
    public static string RemoveFavorite => Get(nameof(RemoveFavorite));
    public static string AddTag => Get(nameof(AddTag));
    public static string TagName => Get(nameof(TagName));
    public static string TagAdded => Get(nameof(TagAdded));
    public static string CreateShareLink => Get(nameof(CreateShareLink));
    public static string ShareExpiration => Get(nameof(ShareExpiration));
    public static string Days => Get(nameof(Days));
    public static string SharePassword => Get(nameof(SharePassword));
    public static string OptionalPassword => Get(nameof(OptionalPassword));
    public static string ShareNotCreated => Get(nameof(ShareNotCreated));
    public static string ExpirationDaysInvalid => Get(nameof(ExpirationDaysInvalid));
    public static string ShareLinkCopied => Get(nameof(ShareLinkCopied));
    public static string ShareLinkCopiedPasswordRequired => Get(nameof(ShareLinkCopiedPasswordRequired));
    public static string InvalidFileDetails => Get(nameof(InvalidFileDetails));
    public static string InvalidMediaId => Get(nameof(InvalidMediaId));
    public static string RestoreItemsOrRemovePermanently => Get(nameof(RestoreItemsOrRemovePermanently));
    public static string TrashIsEmpty => Get(nameof(TrashIsEmpty));
    public static string TrashAlreadyEmpty => Get(nameof(TrashAlreadyEmpty));
    public static string UnableToRestore => Get(nameof(UnableToRestore));
    public static string PermanentlyDeleteQuestion => Get(nameof(PermanentlyDeleteQuestion));
    public static string UnableToPermanentlyDelete => Get(nameof(UnableToPermanentlyDelete));
    public static string EmptyTrash => Get(nameof(EmptyTrash));
    public static string EmptyTrashQuestion => Get(nameof(EmptyTrashQuestion));
    public static string UnableToEmptyTrash => Get(nameof(UnableToEmptyTrash));
    public static string AccountServerPreferences => Get(nameof(AccountServerPreferences));
    public static string Server => Get(nameof(Server));
    public static string Security => Get(nameof(Security));
    public static string LocalSessionProtected => Get(nameof(LocalSessionProtected));
    public static string Wechat => Get(nameof(Wechat));
    public static string Checking => Get(nameof(Checking));
    public static string BindWechat => Get(nameof(BindWechat));
    public static string Unbind => Get(nameof(Unbind));
    public static string UnbindWechat => Get(nameof(UnbindWechat));
    public static string UnbindWechatQuestion => Get(nameof(UnbindWechatQuestion));
    public static string CheckingLocalSession => Get(nameof(CheckingLocalSession));
    public static string SignedInOnThisDevice => Get(nameof(SignedInOnThisDevice));
    public static string NoValidLocalSession => Get(nameof(NoValidLocalSession));
    public static string UnableToReadLocalSession => Get(nameof(UnableToReadLocalSession));
    public static string Unavailable => Get(nameof(Unavailable));
    public static string NotEnabled => Get(nameof(NotEnabled));
    public static string SignInRequired => Get(nameof(SignInRequired));
    public static string NotBound => Get(nameof(NotBound));
    public static string UnavailableOnThisDevice => Get(nameof(UnavailableOnThisDevice));
    public static string Bound => Get(nameof(Bound));
    public static string BoundWithName => Get(nameof(BoundWithName));
    public static string WechatAuthorizationCanceled => Get(nameof(WechatAuthorizationCanceled));
    public static string SignOutQuestion => Get(nameof(SignOutQuestion));
    public static string NoLogsFound => Get(nameof(NoLogsFound));
    public static string LogsEmptyHelp => Get(nameof(LogsEmptyHelp));
    public static string UnknownUser => Get(nameof(UnknownUser));
    public static string UnableToRestoreSignInState => Get(nameof(UnableToRestoreSignInState));
    public static string FileKindFolder => Get(nameof(FileKindFolder));
    public static string FileKindImage => Get(nameof(FileKindImage));
    public static string FileKindVideo => Get(nameof(FileKindVideo));
    public static string FileKindArchive => Get(nameof(FileKindArchive));
    public static string FileKindDocument => Get(nameof(FileKindDocument));
    public static string FileKindFile => Get(nameof(FileKindFile));

    public static void UseDefaultCulture()
    {
        var culture = CultureInfo.GetCultureInfo(DefaultCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string Get(string key)
    {
        if (!Texts.TryGetValue(key, out var value))
        {
            return key;
        }

        return IsChineseCulture(CultureInfo.CurrentUICulture) ? value.Zh : value.En;
    }

    public static string Format(string key, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }

    public static string FileKind(string kind)
    {
        return kind switch
        {
            "Folder" => Get(nameof(FileKindFolder)),
            "Image" => Get(nameof(FileKindImage)),
            "Video" => Get(nameof(FileKindVideo)),
            "Archive" => Get(nameof(FileKindArchive)),
            "Document" => Get(nameof(FileKindDocument)),
            _ => kind == "PDF" ? "PDF" : Get(nameof(FileKindFile))
        };
    }

    public static string UploadStatus(UploadQueueStatus status)
    {
        return status switch
        {
            UploadQueueStatus.Waiting => Waiting,
            UploadQueueStatus.Uploading => Uploading,
            UploadQueueStatus.Completed => Completed,
            UploadQueueStatus.Failed => Failed,
            _ => Unknown
        };
    }

    public static string FormatDate(DateTime dateTime)
    {
        var localTime = dateTime.Kind == DateTimeKind.Utc
            ? dateTime.ToLocalTime()
            : dateTime;

        return IsChineseCulture(CultureInfo.CurrentUICulture)
            ? localTime.ToString("M月d日", CultureInfo.CurrentUICulture)
            : localTime.ToString("MMM d", CultureInfo.CurrentUICulture);
    }

    private static bool IsChineseCulture(CultureInfo culture)
    {
        return culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct TextPair(string Zh, string En);
}
