using System.Collections.ObjectModel;
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
                $"Unable to restore \"{item.Name}\". {exception.Message} If the original folder already has an item with the same name, rename or remove the active item before retrying.");
        }
    }

    private async void OnPermanentDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Delete forever",
            $"Permanently delete \"{item.Name}\"? This cannot be undone.",
            "Delete",
            "Cancel");

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
            await ShowErrorAsync($"Unable to permanently delete \"{item.Name}\". {exception.Message}");
        }
    }

    private async void OnEmptyTrashClicked(object? sender, EventArgs e)
    {
        if (TrashItems.Count == 0)
        {
            await ShowInfoAsync("Trash is already empty.");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Empty trash",
            "Permanently delete all items in trash? This cannot be undone.",
            "Empty",
            "Cancel");

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
            await ShowErrorAsync($"Unable to empty trash. {exception.Message}");
        }
    }

    private async Task LoadTrashAsync()
    {
        SetLoadingState("Loading trash...");

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
