using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 当前用户分享管理页。
/// </summary>
public partial class SharesPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<ShareListItem> Shares { get; } = [];

    /// <summary>
    /// 初始化 <see cref="SharesPage"/> 的新实例。
    /// </summary>
    public SharesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSharesAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadSharesAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ShareListItem item })
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(item.Link);
        await DisplayAlertAsync("分享链接", "链接已复制。", "OK");
    }

    private async void OnDisableClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ShareListItem item })
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "禁用分享",
            $"禁用 {item.FileName} 的分享链接？",
            "禁用",
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.DisableShareAsync(item.Id);
            await LoadSharesAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async Task LoadSharesAsync()
    {
        SetLoadingState("正在读取分享");

        try
        {
            var shares = await _apiClient.GetSharesAsync();
            Shares.Clear();

            foreach (var share in shares)
            {
                Shares.Add(ShareListItem.FromShare(share));
            }

            SetIdleState();
        }
        catch (AuthSessionExpiredException)
        {
            Shares.Clear();
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            Shares.Clear();
            await ShowErrorAsync($"无法读取共享链接。{exception.Message} 请检查网络或服务器状态后重试。");
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

    private Task ShowErrorAsync(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;

        return Task.CompletedTask;
    }

    public sealed class ShareListItem
    {
        public Guid Id { get; init; }

        public string FileName { get; init; } = string.Empty;

        public string Link { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public string CreatedText { get; init; } = string.Empty;

        public string ExpirationText { get; init; } = string.Empty;

        public string VisitText { get; init; } = string.Empty;

        public bool CanDisable { get; init; }

        public static ShareListItem FromShare(CloudDriveShare share)
        {
            return new ShareListItem
            {
                Id = share.Id,
                FileName = share.FileName,
                Link = $"{AppSettings.ApiBaseUrl.TrimEnd('/')}/api/public/shares/{Uri.EscapeDataString(share.Token)}",
                StatusText = GetStatusText(share),
                CreatedText = $"创建 {AppText.FormatDate(share.CreationTime)}",
                ExpirationText = share.ExpirationTime.HasValue
                    ? $"到期 {AppText.FormatDate(share.ExpirationTime.Value)}"
                    : "长期有效",
                VisitText = $"{share.VisitCount} 次访问",
                CanDisable = share.IsEnabled && !share.IsExpired
            };
        }

        private static string GetStatusText(CloudDriveShare share)
        {
            if (!share.IsEnabled)
            {
                return "已禁用";
            }

            if (share.IsExpired)
            {
                return "已过期";
            }

            return share.RequiresPassword ? "有效，需要密码" : "有效";
        }
    }
}
