using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class OperationLogsPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<CloudOperationLog> Logs { get; } = [];

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

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
        SetLoadingState("Loading operation logs...");

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
            SetErrorState($"Unable to load operation logs. {exception.Message}");
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
