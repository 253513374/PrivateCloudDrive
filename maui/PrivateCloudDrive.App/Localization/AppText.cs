using System.Globalization;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Localization;

/// <summary>
/// 表示AppText组件，封装对应业务场景的状态或行为。
/// </summary>
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
        [nameof(SignInWithGoogle)] = new("\u4f7f\u7528 Google \u767b\u5f55", "Sign in with Google"),
        [nameof(SignInWithGitHub)] = new("\u4f7f\u7528 GitHub \u767b\u5f55", "Sign in with GitHub"),
        [nameof(UserNameOrEmail)] = new("用户名或邮箱", "Username or email"),
        [nameof(Password)] = new("密码", "Password"),
        [nameof(EnterUserNameAndPassword)] = new("请输入用户名和密码。", "Enter username and password."),
        [nameof(EnterUserNamePasswordThenWechat)] = new("请输入用户名和密码，然后再次使用微信完成绑定。", "Enter username and password, then use WeChat again to bind."),
        [nameof(EnterUserNamePasswordThenExternal)] = new("\u8bf7\u8f93\u5165\u7528\u6237\u540d\u548c\u5bc6\u7801\uff0c\u7136\u540e\u518d\u6b21\u4f7f\u7528\u7b2c\u4e09\u65b9\u8d26\u53f7\u5b8c\u6210\u7ed1\u5b9a\u3002", "Enter username and password, then use the external account again to bind."),
        [nameof(WechatSignInCanceled)] = new("微信登录已取消。", "WeChat sign-in was canceled."),
        [nameof(WechatSignInFailed)] = new("微信登录失败。", "WeChat sign-in failed."),
        [nameof(WechatSignInNotEnabled)] = new("微信登录未启用", "WeChat sign-in is not enabled"),
        [nameof(WechatUnavailableOnThisDevice)] = new("此设备未安装微信或暂不可用", "WeChat is not installed or unavailable on this device"),
        [nameof(UnableToLoadWechatSettings)] = new("无法读取微信登录配置", "Unable to load WeChat sign-in settings"),
        [nameof(ExternalSignInCanceled)] = new("\u7b2c\u4e09\u65b9\u767b\u5f55\u5df2\u53d6\u6d88\u3002", "External sign-in was canceled."),
        [nameof(ExternalSignInFailed)] = new("\u7b2c\u4e09\u65b9\u767b\u5f55\u5931\u8d25\u3002", "External sign-in failed."),
        [nameof(ExternalSignInTimedOut)] = new("\u7b2c\u4e09\u65b9\u767b\u5f55\u672a\u5b8c\u6210\uff0c\u8bf7\u68c0\u67e5\u7f51\u7edc\u540e\u91cd\u8bd5\u3002", "External sign-in did not finish. Check the network and try again."),
        [nameof(ExternalSignInNotEnabled)] = new("\u7b2c\u4e09\u65b9\u767b\u5f55\u672a\u542f\u7528", "External sign-in is not enabled"),
        [nameof(UnableToLoadExternalSettings)] = new("\u65e0\u6cd5\u8bfb\u53d6\u7b2c\u4e09\u65b9\u767b\u5f55\u914d\u7f6e", "Unable to load external sign-in settings"),
        [nameof(Files)] = new("文件", "Files"),
        [nameof(MediaLibrary)] = new("媒体库", "Library"),
        [nameof(Albums)] = new("相册", "Albums"),
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
        [nameof(Google)] = new("Google", "Google"),
        [nameof(GitHub)] = new("GitHub", "GitHub"),
        [nameof(Checking)] = new("正在检查", "Checking"),
        [nameof(BindWechat)] = new("绑定微信", "Bind WeChat"),
        [nameof(BindGoogle)] = new("\u7ed1\u5b9a Google", "Bind Google"),
        [nameof(BindGitHub)] = new("\u7ed1\u5b9a GitHub", "Bind GitHub"),
        [nameof(Unbind)] = new("解绑", "Unbind"),
        [nameof(UnbindWechat)] = new("解绑微信", "Unbind WeChat"),
        [nameof(UnbindWechatQuestion)] = new("从此账号解绑微信？", "Unbind WeChat from this account?"),
        [nameof(UnbindGoogle)] = new("\u89e3\u7ed1 Google", "Unbind Google"),
        [nameof(UnbindGitHub)] = new("\u89e3\u7ed1 GitHub", "Unbind GitHub"),
        [nameof(UnbindGoogleQuestion)] = new("\u4ece\u6b64\u8d26\u53f7\u89e3\u7ed1 Google\uff1f", "Unbind Google from this account?"),
        [nameof(UnbindGitHubQuestion)] = new("\u4ece\u6b64\u8d26\u53f7\u89e3\u7ed1 GitHub\uff1f", "Unbind GitHub from this account?"),
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
        [nameof(ExternalAuthorizationCanceled)] = new("\u7b2c\u4e09\u65b9\u6388\u6743\u5df2\u53d6\u6d88\u3002", "External authorization was canceled."),
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

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Start => Get(nameof(Start));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignIn => Get(nameof(SignIn));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignInAction => Get(nameof(SignInAction));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SigningIn => Get(nameof(SigningIn));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string CheckingSignInStatus => Get(nameof(CheckingSignInStatus));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string RestoringSession => Get(nameof(RestoringSession));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string StartupFailed => Get(nameof(StartupFailed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignInWithWechat => Get(nameof(SignInWithWechat));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignInWithGoogle => Get(nameof(SignInWithGoogle));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignInWithGitHub => Get(nameof(SignInWithGitHub));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UserNameOrEmail => Get(nameof(UserNameOrEmail));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Password => Get(nameof(Password));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string EnterUserNameAndPassword => Get(nameof(EnterUserNameAndPassword));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string EnterUserNamePasswordThenWechat => Get(nameof(EnterUserNamePasswordThenWechat));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string EnterUserNamePasswordThenExternal => Get(nameof(EnterUserNamePasswordThenExternal));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string WechatSignInCanceled => Get(nameof(WechatSignInCanceled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string WechatSignInFailed => Get(nameof(WechatSignInFailed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string WechatSignInNotEnabled => Get(nameof(WechatSignInNotEnabled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string WechatUnavailableOnThisDevice => Get(nameof(WechatUnavailableOnThisDevice));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadWechatSettings => Get(nameof(UnableToLoadWechatSettings));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ExternalSignInCanceled => Get(nameof(ExternalSignInCanceled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ExternalSignInFailed => Get(nameof(ExternalSignInFailed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ExternalSignInTimedOut => Get(nameof(ExternalSignInTimedOut));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ExternalSignInNotEnabled => Get(nameof(ExternalSignInNotEnabled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadExternalSettings => Get(nameof(UnableToLoadExternalSettings));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Files => Get(nameof(Files));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string MediaLibrary => Get(nameof(MediaLibrary));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Albums => Get(nameof(Albums));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Photos => Get(nameof(Photos));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Videos => Get(nameof(Videos));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Uploads => Get(nameof(Uploads));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Trash => Get(nameof(Trash));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Settings => Get(nameof(Settings));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Details => Get(nameof(Details));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string OperationLogs => Get(nameof(OperationLogs));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Refresh => Get(nameof(Refresh));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Retry => Get(nameof(Retry));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Back => Get(nameof(Back));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string More => Get(nameof(More));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Delete => Get(nameof(Delete));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Logout => Get(nameof(Logout));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignOut => Get(nameof(SignOut));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Cancel => Get(nameof(Cancel));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Create => Get(nameof(Create));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Add => Get(nameof(Add));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Next => Get(nameof(Next));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Move => Get(nameof(Move));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Empty => Get(nameof(Empty));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Restore => Get(nameof(Restore));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string DeleteForever => Get(nameof(DeleteForever));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NewFolder => Get(nameof(NewFolder));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NewFolderLower => Get(nameof(NewFolderLower));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FolderName => Get(nameof(FolderName));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Upload => Get(nameof(Upload));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ThisFolderIsEmpty => Get(nameof(ThisFolderIsEmpty));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string EmptyFolderHelp => Get(nameof(EmptyFolderHelp));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LoadingFiles => Get(nameof(LoadingFiles));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LoadingPhotos => Get(nameof(LoadingPhotos));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LoadingVideos => Get(nameof(LoadingVideos));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LoadingTrash => Get(nameof(LoadingTrash));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LoadingOperationLogs => Get(nameof(LoadingOperationLogs));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadFiles => Get(nameof(UnableToLoadFiles));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadPhotos => Get(nameof(UnableToLoadPhotos));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadVideos => Get(nameof(UnableToLoadVideos));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadOperationLogs => Get(nameof(UnableToLoadOperationLogs));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SomeUploadsFailed => Get(nameof(SomeUploadsFailed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UploadFailed => Get(nameof(UploadFailed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UploadFailedBeforeRequest => Get(nameof(UploadFailedBeforeRequest));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SelectFilesToUpload => Get(nameof(SelectFilesToUpload));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string AllFilesFilter => Get(nameof(AllFilesFilter));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string WindowsFileDialogFailed => Get(nameof(WindowsFileDialogFailed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToCreateFolder => Get(nameof(UnableToCreateFolder));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToDelete => Get(nameof(UnableToDelete));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string MoveToTrash => Get(nameof(MoveToTrash));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string MoveToTrashQuestion => Get(nameof(MoveToTrashQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoPhotosFound => Get(nameof(NoPhotosFound));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoPhotosHelp => Get(nameof(NoPhotosHelp));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoVideosFound => Get(nameof(NoVideosFound));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoVideosHelp => Get(nameof(NoVideosHelp));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string PhotosCount => Get(nameof(PhotosCount));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string VideosCount => Get(nameof(VideosCount));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ActiveFailedCompletedUploads => Get(nameof(ActiveFailedCompletedUploads));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoUploadTasks => Get(nameof(NoUploadTasks));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoUploadTasksHelp => Get(nameof(NoUploadTasksHelp));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ClearDone => Get(nameof(ClearDone));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UploadQueueEmpty => Get(nameof(UploadQueueEmpty));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UploadQueueSummary => Get(nameof(UploadQueueSummary));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Completed => Get(nameof(Completed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Waiting => Get(nameof(Waiting));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Uploading => Get(nameof(Uploading));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Failed => Get(nameof(Failed));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Unknown => Get(nameof(Unknown));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Type => Get(nameof(Type));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Size => Get(nameof(Size));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Modified => Get(nameof(Modified));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string AvailableActions => Get(nameof(AvailableActions));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LoadingImagePreview => Get(nameof(LoadingImagePreview));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToLoadImagePreview => Get(nameof(UnableToLoadImagePreview));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Favorited => Get(nameof(Favorited));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NotFavorited => Get(nameof(NotFavorited));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string AddFavorite => Get(nameof(AddFavorite));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string RemoveFavorite => Get(nameof(RemoveFavorite));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string AddTag => Get(nameof(AddTag));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string TagName => Get(nameof(TagName));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string TagAdded => Get(nameof(TagAdded));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string CreateShareLink => Get(nameof(CreateShareLink));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ShareExpiration => Get(nameof(ShareExpiration));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Days => Get(nameof(Days));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SharePassword => Get(nameof(SharePassword));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string OptionalPassword => Get(nameof(OptionalPassword));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ShareNotCreated => Get(nameof(ShareNotCreated));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ExpirationDaysInvalid => Get(nameof(ExpirationDaysInvalid));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ShareLinkCopied => Get(nameof(ShareLinkCopied));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ShareLinkCopiedPasswordRequired => Get(nameof(ShareLinkCopiedPasswordRequired));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string InvalidFileDetails => Get(nameof(InvalidFileDetails));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string InvalidMediaId => Get(nameof(InvalidMediaId));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string RestoreItemsOrRemovePermanently => Get(nameof(RestoreItemsOrRemovePermanently));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string TrashIsEmpty => Get(nameof(TrashIsEmpty));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string TrashAlreadyEmpty => Get(nameof(TrashAlreadyEmpty));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToRestore => Get(nameof(UnableToRestore));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string PermanentlyDeleteQuestion => Get(nameof(PermanentlyDeleteQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToPermanentlyDelete => Get(nameof(UnableToPermanentlyDelete));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string EmptyTrash => Get(nameof(EmptyTrash));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string EmptyTrashQuestion => Get(nameof(EmptyTrashQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToEmptyTrash => Get(nameof(UnableToEmptyTrash));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string AccountServerPreferences => Get(nameof(AccountServerPreferences));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Server => Get(nameof(Server));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Security => Get(nameof(Security));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LocalSessionProtected => Get(nameof(LocalSessionProtected));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Wechat => Get(nameof(Wechat));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Google => Get(nameof(Google));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string GitHub => Get(nameof(GitHub));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Checking => Get(nameof(Checking));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string BindWechat => Get(nameof(BindWechat));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string BindGoogle => Get(nameof(BindGoogle));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string BindGitHub => Get(nameof(BindGitHub));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Unbind => Get(nameof(Unbind));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnbindWechat => Get(nameof(UnbindWechat));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnbindWechatQuestion => Get(nameof(UnbindWechatQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnbindGoogle => Get(nameof(UnbindGoogle));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnbindGitHub => Get(nameof(UnbindGitHub));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnbindGoogleQuestion => Get(nameof(UnbindGoogleQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnbindGitHubQuestion => Get(nameof(UnbindGitHubQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string CheckingLocalSession => Get(nameof(CheckingLocalSession));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignedInOnThisDevice => Get(nameof(SignedInOnThisDevice));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoValidLocalSession => Get(nameof(NoValidLocalSession));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToReadLocalSession => Get(nameof(UnableToReadLocalSession));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Unavailable => Get(nameof(Unavailable));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NotEnabled => Get(nameof(NotEnabled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignInRequired => Get(nameof(SignInRequired));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NotBound => Get(nameof(NotBound));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnavailableOnThisDevice => Get(nameof(UnavailableOnThisDevice));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Bound => Get(nameof(Bound));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string BoundWithName => Get(nameof(BoundWithName));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string WechatAuthorizationCanceled => Get(nameof(WechatAuthorizationCanceled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string ExternalAuthorizationCanceled => Get(nameof(ExternalAuthorizationCanceled));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string SignOutQuestion => Get(nameof(SignOutQuestion));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string NoLogsFound => Get(nameof(NoLogsFound));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string LogsEmptyHelp => Get(nameof(LogsEmptyHelp));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnknownUser => Get(nameof(UnknownUser));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string UnableToRestoreSignInState => Get(nameof(UnableToRestoreSignInState));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FileKindFolder => Get(nameof(FileKindFolder));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FileKindImage => Get(nameof(FileKindImage));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FileKindVideo => Get(nameof(FileKindVideo));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FileKindArchive => Get(nameof(FileKindArchive));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FileKindDocument => Get(nameof(FileKindDocument));
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string FileKindFile => Get(nameof(FileKindFile));

    /// <summary>
    /// 执行UseDefaultCulture操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static void UseDefaultCulture()
    {
        var culture = CultureInfo.GetCultureInfo(DefaultCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string Get(string key)
    {
        if (!Texts.TryGetValue(key, out var value))
        {
            return key;
        }

        return IsChineseCulture(CultureInfo.CurrentUICulture) ? value.Zh : value.En;
    }

    /// <summary>
    /// 执行Format操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static string Format(string key, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }

    /// <summary>
    /// 执行FileKind操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
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

    /// <summary>
    /// 处理文件上传或保存请求，校验大小、归属和存储一致性后写入数据。
    /// </summary>
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

    /// <summary>
    /// 执行FormatDate操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
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
