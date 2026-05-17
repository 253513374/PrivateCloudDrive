namespace PrivateCloudDrive.App.Views;

/// <summary>
/// Open Design storage usage preview screen.
/// </summary>
public partial class StorageUsagePage : ContentPage
{
    public StorageUsagePage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnAiCleanupClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("AI 清理", "智能整理会在后续版本接入真实分析任务。", "知道了");
    }
}
