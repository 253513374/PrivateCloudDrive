using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示OperationLogsPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class OperationLogsPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<CloudOperationLog> Logs { get; } = [];

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    /// <summary>
    /// 初始化 <see cref="OperationLogsPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public OperationLogsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLogsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadLogsAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        RefreshButton.IsEnabled = false;
        SetLoadingState(AppText.LoadingOperationLogs);

        try
        {
            var logs = await _apiClient.GetOperationLogsAsync();
            Logs.Clear();

            foreach (var log in logs)
            {
                Logs.Add(log);
            }

            SetIdleState();
        }
        catch (Exception exception)
        {
            Logs.Clear();
            SetErrorState(AppText.Format(nameof(AppText.UnableToLoadOperationLogs), exception.Message));
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void SetLoadingState(string message)
    {
        LogsStatePanel.IsVisible = true;
        LogsLoadingIndicator.IsVisible = true;
        LogsLoadingIndicator.IsRunning = true;
        LogsRetryButton.IsVisible = false;
        LogsStateLabel.Text = message;
    }

    private void SetErrorState(string message)
    {
        LogsStatePanel.IsVisible = true;
        LogsLoadingIndicator.IsRunning = false;
        LogsLoadingIndicator.IsVisible = false;
        LogsRetryButton.IsVisible = true;
        LogsStateLabel.Text = message;
    }

    private void SetIdleState()
    {
        LogsStatePanel.IsVisible = false;
        LogsLoadingIndicator.IsRunning = false;
        LogsLoadingIndicator.IsVisible = false;
        LogsRetryButton.IsVisible = false;
    }
}
