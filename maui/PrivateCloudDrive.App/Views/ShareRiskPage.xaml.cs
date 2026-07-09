using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 分享风险提示页，展示无过期分享、公开分享和长期未使用分享的数量与文案。
/// </summary>
public partial class ShareRiskPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ShareRiskPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRiskSummaryAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadRiskSummaryAsync();
    }

    private async void OnSharesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("shares", true);
    }

    private async Task LoadRiskSummaryAsync()
    {
        SetLoadingState("正在读取分享安全状态");

        try
        {
            var summary = await _apiClient.GetShareRiskSummaryAsync();

            NoExpiryCountLabel.Text = $"{summary.NoExpiryShareCount}";
            NoExpiryWarningLabel.Text = string.IsNullOrWhiteSpace(summary.NoExpiryWarning)
                ? "暂无无过期时间的分享。"
                : summary.NoExpiryWarning;

            PublicShareCountLabel.Text = $"{summary.PublicShareCount}";
            PublicShareWarningLabel.Text = string.IsNullOrWhiteSpace(summary.PublicWarning)
                ? "暂无公开分享。"
                : summary.PublicWarning;

            LongUnusedCountLabel.Text = $"{summary.LongUnusedShareCount}";
            LongUnusedWarningLabel.Text = string.IsNullOrWhiteSpace(summary.LongUnusedWarning)
                ? "暂无长期未使用的分享。"
                : summary.LongUnusedWarning;

            SetIdleState();
        }
        catch (Exception exception)
        {
            SetErrorState($"无法读取分享安全状态。{UserVisibleErrorSanitizer.ForSettings(exception)}");
        }
    }

    private void SetLoadingState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RetryButton.IsVisible = false;
        StateLabel.Text = message;
    }

    private void SetIdleState()
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = false;
        StatePanel.IsVisible = false;
    }

    private void SetErrorState(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;
    }
}
