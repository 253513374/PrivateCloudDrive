using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// Open Design quick create action surface.
/// </summary>
public partial class CreateActionPage : ContentPage
{
    private readonly IBackupTransferService _backupTransferService = AppServices.GetRequiredService<IBackupTransferService>();
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();

    public CreateActionPage()
    {
        InitializeComponent();
    }

    private static Task GoToFilesAsync()
    {
        return Shell.Current.GoToAsync("//main/files", true);
    }

    private static Task GoToBackupsAsync()
    {
        return Shell.Current.GoToAsync("//main/uploads", true);
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await GoToFilesAsync();
    }

    private async void OnBackupMediaClicked(object? sender, EventArgs e)
    {
        await StartBackupAsync(PickMediaFilesAsync);
    }

    private async void OnBackupFilesClicked(object? sender, EventArgs e)
    {
        await StartBackupAsync(PickFilesAsync);
    }

    private async void OnBackupQueueClicked(object? sender, EventArgs e)
    {
        await GoToBackupsAsync();
    }

    private async void OnStorageHealthClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("storage-usage", true);
    }

    private async void OnRestoreGuideClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "恢复边界说明",
            "App 负责把本机照片、视频和文件备份到当前私有后端；真正恢复服务器文件时，需要同时恢复数据库、文件存储和部署配置。当前页面不会展示 bucket、服务器绝对路径、连接串、AccessKey 或 Token。",
            "知道了");
    }

    private async Task StartBackupAsync(Func<Task<IReadOnlyList<FileResult>>> pickFiles)
    {
        try
        {
            if (!await _authService.IsSignedInAsync())
            {
                await RedirectToLoginAsync();
                return;
            }

            var files = await pickFiles();
            if (files.Count == 0)
            {
                return;
            }

            var queueItems = await _backupTransferService.BackupFilesAsync(
                targetFolderId: null,
                targetPath: "文件 / 根目录",
                files);

            var failedItems = queueItems.Where(item => item.IsFailed).ToList();
            if (failedItems.Count > 0)
            {
                await DisplayAlertAsync(
                    AppText.SomeUploadsFailed,
                    string.Join(Environment.NewLine, failedItems.Select(item => $"{item.FileName}: {item.ErrorMessage}")),
                    "OK");
            }

            await GoToBackupsAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (AuthSessionExpiredException)
        {
            await RedirectToLoginAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync(AppText.UploadFailed, exception.Message, "OK");
        }
    }

    private async Task RedirectToLoginAsync()
    {
        await _authService.SignOutAsync();
        await DisplayAlertAsync(AppText.SignInRequired, "请先登录后再开始备份。", "OK");
        await Shell.Current.GoToAsync("//login", true);
    }

    private static async Task<IReadOnlyList<FileResult>> PickFilesAsync()
    {
        var pickedFiles = await FilePicker.Default.PickMultipleAsync(new PickOptions
        {
            PickerTitle = AppText.SelectFilesToUpload
        });

        return pickedFiles?.OfType<FileResult>().ToList() ?? [];
    }

    private static async Task<IReadOnlyList<FileResult>> PickMediaFilesAsync()
    {
        var pickedFiles = await FilePicker.Default.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "选择要备份的照片或视频",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = ["image/*", "video/*"],
                [DevicePlatform.iOS] = ["public.image", "public.movie"],
                [DevicePlatform.MacCatalyst] = ["public.image", "public.movie"],
                [DevicePlatform.WinUI] = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".m4v", ".webm"]
            })
        });

        return pickedFiles?.OfType<FileResult>().ToList() ?? [];
    }

    private async void OnNewFolderClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("新建文件夹", "请在文件页点击“新建文件夹”完成创建。", "知道了");
        await GoToFilesAsync();
    }

    private async void OnComingSoonClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("即将推出", "这个 Open Design 动作正在探索中。", "知道了");
    }
}
