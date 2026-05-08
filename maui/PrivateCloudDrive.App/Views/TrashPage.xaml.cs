using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class TrashPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<CloudDriveItem> TrashItems { get; } = [];

    public TrashPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTrashAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadTrashAsync();
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        try
        {
            await _apiClient.RestoreTrashItemAsync(item.Id);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(
                AppText.Format(nameof(AppText.UnableToRestore), item.Name, exception.Message));
        }
    }

    private async void OnPermanentDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.DeleteForever,
            AppText.Format(nameof(AppText.PermanentlyDeleteQuestion), item.Name),
            AppText.Delete,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.PermanentlyDeleteTrashItemAsync(item.Id);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(AppText.Format(nameof(AppText.UnableToPermanentlyDelete), item.Name, exception.Message));
        }
    }

    private async void OnEmptyTrashClicked(object? sender, EventArgs e)
    {
        if (TrashItems.Count == 0)
        {
            await ShowInfoAsync(AppText.TrashAlreadyEmpty);
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.EmptyTrash,
            AppText.EmptyTrashQuestion,
            AppText.Empty,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.EmptyTrashAsync();
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(AppText.Format(nameof(AppText.UnableToEmptyTrash), exception.Message));
        }
    }

    private async Task LoadTrashAsync()
    {
        SetLoadingState(AppText.LoadingTrash);

        try
        {
            var items = await _apiClient.GetTrashItemsAsync();
            TrashItems.Clear();

            foreach (var item in items)
            {
                TrashItems.Add(item);
            }

            SetIdleState();
        }
        catch (Exception exception)
        {
            TrashItems.Clear();
            await ShowErrorAsync(exception.Message);
        }
    }

    private void SetLoadingState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        TrashRetryButton.IsVisible = false;
        StateLabel.Text = message;
    }

    private void SetIdleState()
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        TrashRetryButton.IsVisible = false;
        StatePanel.IsVisible = false;
    }

    private Task ShowInfoAsync(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        TrashRetryButton.IsVisible = false;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;

        return Task.CompletedTask;
    }

    private Task ShowErrorAsync(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        TrashRetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;

        return Task.CompletedTask;
    }
}
