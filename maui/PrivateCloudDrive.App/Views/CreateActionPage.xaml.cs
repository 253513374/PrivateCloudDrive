namespace PrivateCloudDrive.App.Views;

/// <summary>
/// Open Design quick create action surface.
/// </summary>
public partial class CreateActionPage : ContentPage
{
    public CreateActionPage()
    {
        InitializeComponent();
    }

    private static Task GoToFilesAsync()
    {
        return Shell.Current.GoToAsync("//main/files", true);
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await GoToFilesAsync();
    }

    private async void OnUploadActionClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("继续上传", "请在文件页点击上传按钮选择本机文件。", "知道了");
        await GoToFilesAsync();
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
