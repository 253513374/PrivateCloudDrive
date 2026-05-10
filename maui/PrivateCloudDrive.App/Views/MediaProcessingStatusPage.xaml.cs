using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class MediaProcessingStatusPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private string? _statusFilter;

    public ObservableCollection<MediaLibraryItem> Items { get; } = [];

    public string ItemCountText => $"{Items.Count} 项待处理媒体";

    public MediaProcessingStatusPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadItemsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnAllClicked(object? sender, EventArgs e)
    {
        _statusFilter = null;
        await LoadItemsAsync();
    }

    private async void OnPendingClicked(object? sender, EventArgs e)
    {
        _statusFilter = "Pending";
        await LoadItemsAsync();
    }

    private async void OnProcessingClicked(object? sender, EventArgs e)
    {
        _statusFilter = "Processing";
        await LoadItemsAsync();
    }

    private async void OnFailedClicked(object? sender, EventArgs e)
    {
        _statusFilter = "Failed";
        await LoadItemsAsync();
    }

    private async void OnCompletedClicked(object? sender, EventArgs e)
    {
        _statusFilter = "Completed";
        await LoadItemsAsync();
    }

    private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }

        if (e.CurrentSelection.FirstOrDefault() is not MediaLibraryItem item)
        {
            return;
        }

        var route = $"media-preview?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={item.Kind}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async void OnRetryProcessingClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not Guid fileNodeId)
        {
            return;
        }

        try
        {
            button.IsEnabled = false;
            await _apiClient.RetryMediaProcessingAsync(fileNodeId);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法重新处理", exception.Message, "确定");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async Task LoadItemsAsync()
    {
        SetLoadingState("正在加载处理状态...");

        try
        {
            var items = await _apiClient.GetMediaProcessingItemsAsync(_statusFilter);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new MediaLibraryItem(item));
            }

            OnPropertyChanged(nameof(ItemCountText));
            UpdateSegmentButtons();
            SetIdleState();
            await LoadThumbnailsAsync();
        }
        catch (Exception exception)
        {
            Items.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            UpdateSegmentButtons();
            SetErrorState($"无法加载处理状态。{exception.Message}");
        }
    }

    private async Task LoadThumbnailsAsync()
    {
        foreach (var item in Items)
        {
            try
            {
                var content = await _apiClient.GetFileContentAsync(item.Id, thumbnail: true);
                item.ThumbnailSource = ImageSource.FromStream(() => new MemoryStream(content.Content));
            }
            catch
            {
                // Keep the badge fallback visible while thumbnails are missing or processing.
            }
        }
    }

    private void SetLoadingState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        StateLabel.Text = message;
    }

    private void SetErrorState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        StateLabel.Text = message;
    }

    private void SetIdleState()
    {
        StatePanel.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    private void UpdateSegmentButtons()
    {
        SetSegmentButton(AllStatusButton, _statusFilter is null);
        SetSegmentButton(ProcessingStatusButton, _statusFilter == "Processing");
        SetSegmentButton(FailedStatusButton, _statusFilter == "Failed");
        SetSegmentButton(CompletedStatusButton, _statusFilter == "Completed");
    }

    private void SetSegmentButton(Button button, bool isSelected)
    {
        if (Application.Current?.Resources is null)
        {
            return;
        }

        button.Style = (Style)Application.Current.Resources[
            isSelected ? "SegmentButtonSelected" : "SegmentButton"];
    }
}
