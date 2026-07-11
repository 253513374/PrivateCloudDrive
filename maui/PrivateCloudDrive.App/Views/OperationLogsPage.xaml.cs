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
    /// 表示操作类型选项列表，用于 Picker 数据源。
    /// </summary>
    private static readonly List<ActionFilterOption> ActionOptions =
    [
        new(null, AppText.AllActions),
        new("FileUpload", AppText.ActionFileUpload),
        new("FileDownload", AppText.ActionFileDownload),
        new("FileDelete", AppText.ActionFileDelete),
        new("FileRestore", AppText.ActionFileRestore),
        new("FilePermanentDelete", AppText.ActionFilePermanentDelete),
        new("TrashEmpty", AppText.ActionTrashEmpty),
        new("FolderCreate", AppText.ActionFolderCreate),
        new("ShareCreate", AppText.ActionShareCreate),
        new("ShareDelete", AppText.ActionShareDelete),
        new("ShareAccess", AppText.ActionShareAccess),
        new("ShareDownload", AppText.ActionShareDownload),
        new("TagCreate", AppText.ActionTagCreate),
        new("FavoriteSet", AppText.ActionFavoriteSet),
        new("Security", AppText.ActionSecurity),
        new("AdminCreateUser", AppText.ActionAdminUser),
    ];

    /// <summary>
    /// 初始化 <see cref="OperationLogsPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public OperationLogsPage()
    {
        InitializeComponent();
        BindingContext = this;

        // 填充 Picker
        ActionPicker.ItemsSource = ActionOptions;
        ActionPicker.ItemDisplayBinding = new Binding("DisplayName");
        ActionPicker.SelectedIndex = 0;
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

    private async void OnApplyFilterClicked(object? sender, EventArgs e)
    {
        await LoadLogsAsync();
    }

    private async void OnResetFilterClicked(object? sender, EventArgs e)
    {
        UserNameEntry.Text = string.Empty;
        ActionPicker.SelectedIndex = 0;
        StartDatePicker.Date = DateTime.Today.AddMonths(-1);
        EndDatePicker.Date = DateTime.Today;
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        RefreshButton.IsEnabled = false;
        ApplyFilterButton.IsEnabled = false;
        ResetFilterButton.IsEnabled = false;
        SetLoadingState(AppText.LoadingOperationLogs);

        try
        {
            var actionOption = ActionPicker.SelectedItem as ActionFilterOption;
            var userName = string.IsNullOrWhiteSpace(UserNameEntry.Text) ? null : UserNameEntry.Text.Trim();
            var action = actionOption?.ActionValue;

            // DatePicker 默认值处理：仅当用户明确选择了日期才发送
            var today = DateTime.Today;
            DateTime? startTime = null;
            DateTime? endTime = null;

            if (StartDatePicker.Date.HasValue && StartDatePicker.Date.Value != today.AddMonths(-1) && StartDatePicker.Date.Value != today)
                startTime = StartDatePicker.Date.Value;
            if (EndDatePicker.Date.HasValue && EndDatePicker.Date.Value != today)
            {
                endTime = EndDatePicker.Date.Value.AddDays(1).AddTicks(-1); // 当天结束时间
            }
            var logs = await _apiClient.GetOperationLogsAsync(
                userName: userName,
                action: action,
                startTime: startTime,
                endTime: endTime);

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
            ApplyFilterButton.IsEnabled = true;
            ResetFilterButton.IsEnabled = true;
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

    /// <summary>
    /// 表示操作类型筛选选项。
    /// </summary>
    private sealed record ActionFilterOption(string? ActionValue, string DisplayName);
}
