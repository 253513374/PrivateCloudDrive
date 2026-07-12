using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 存储配置展示页（只读），通过管理员 API 获取存储后端、路径、容量等信息。
/// </summary>
public partial class StorageConfigPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();

    public StorageConfigPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStorageConfigAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadStorageConfigAsync();
    }

    private async Task LoadStorageConfigAsync()
    {
        SetLoadingState();

        try
        {
            var isSignedIn = await _authService.IsSignedInAsync();
            if (!isSignedIn)
            {
                SetSignedOutState();
                return;
            }

            var config = await _apiClient.GetStorageConfigAsync();
            SetDataState(config);
        }
        catch (AuthSessionExpiredException)
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            SetErrorState(UserVisibleErrorSanitizer.ForSettings(exception));
        }
    }

    private void SetLoadingState()
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RetryButton.IsVisible = false;
        StateLabel.Text = "正在读取存储配置";

        StorageProviderLabel.Text = "--";
        StoragePathLabel.Text = "--";
        TotalBytesLabel.Text = "--";
        UsedBytesLabel.Text = "--";
        AvailableBytesLabel.Text = "--";
        MaxSingleFileSizeLabel.Text = "--";
    }

    private void SetSignedOutState()
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = false;
        StatePanel.IsVisible = true;
        StateLabel.Text = AppText.SignInRequired;

        StorageProviderLabel.Text = "不可用";
        StoragePathLabel.Text = "不可用";
        TotalBytesLabel.Text = "--";
        UsedBytesLabel.Text = "--";
        AvailableBytesLabel.Text = "--";
        MaxSingleFileSizeLabel.Text = "--";
    }

    private void SetDataState(StorageConfigDto config)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = false;
        StatePanel.IsVisible = false;

        StorageProviderLabel.Text = UserVisibleErrorSanitizer.RedactUserVisibleText(config.StorageProvider, "当前存储后端");
        StoragePathLabel.Text = UserVisibleErrorSanitizer.RedactUserVisibleText(config.StoragePath, "服务器已返回存储路径说明，详细位置已隐藏");
        TotalBytesLabel.Text = config.TotalBytes > 0 ? FormatBytes(config.TotalBytes) : "--";
        UsedBytesLabel.Text = FormatBytes(config.UsedBytes);
        AvailableBytesLabel.Text = config.AvailableBytes > 0 ? FormatBytes(config.AvailableBytes) : "--";
        MaxSingleFileSizeLabel.Text = config.MaxSingleFileSize > 0
            ? FormatBytes(config.MaxSingleFileSize)
            : "未限制";
    }

    private void SetErrorState(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(bytes, 0);
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }
}
