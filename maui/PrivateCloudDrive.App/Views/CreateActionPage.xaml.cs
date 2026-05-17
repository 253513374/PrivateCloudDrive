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

    private async Task StartBackupAsync(Func<Task<IReadOnlyList<FileResult>>> pickFiles)
    {
        try
        {
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
        catch (Exception exception)
        {
            await DisplayAlertAsync(AppText.UploadFailed, exception.Message, "OK");
        }
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
