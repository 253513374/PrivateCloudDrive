using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class PhotosPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<MediaLibraryItem> Items { get; } = [];

    public string ItemCountText => AppText.Format(nameof(AppText.PhotosCount), Items.Count);

    public PhotosPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPhotosAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadPhotosAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadPhotosAsync();
    }

    private async void OnPhotoSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MediaLibraryItem item)
        {
            return;
        }

        PhotosCollectionView.SelectedItem = null;
        var route = $"media-preview?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind=Image";
        await Shell.Current.GoToAsync(route, true);
    }

    private async Task LoadPhotosAsync()
    {
        RefreshButton.IsEnabled = false;
        SetLoadingState(AppText.LoadingPhotos);

        try
        {
            var photos = await _apiClient.GetImagesAsync();
            Items.Clear();

            foreach (var photo in photos)
            {
                Items.Add(new MediaLibraryItem(photo));
            }

            OnPropertyChanged(nameof(ItemCountText));
            SetIdleState();
            await LoadThumbnailsAsync();
        }
        catch (Exception exception)
        {
            Items.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            SetErrorState(AppText.Format(nameof(AppText.UnableToLoadPhotos), exception.Message));
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task LoadThumbnailsAsync()
    {
        foreach (var item in Items)
        {
            try
            {
                var content = await GetThumbnailOrImageAsync(item.Id);
                var bytes = content.Content;
                item.ThumbnailSource = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                // Keep the badge fallback visible when thumbnail processing is not ready.
            }
        }
    }

    private async Task<FileContentResult> GetThumbnailOrImageAsync(Guid id)
    {
        try
        {
            return await _apiClient.GetFileContentAsync(id, thumbnail: true);
        }
        catch
        {
            return await _apiClient.GetFileContentAsync(id, thumbnail: false);
        }
    }

    private void SetLoadingState(string message)
    {
        PhotosStatePanel.IsVisible = true;
        PhotosLoadingIndicator.IsVisible = true;
        PhotosLoadingIndicator.IsRunning = true;
        PhotosRetryButton.IsVisible = false;
        PhotosStateLabel.Text = message;
    }

    private void SetErrorState(string message)
    {
        PhotosStatePanel.IsVisible = true;
        PhotosLoadingIndicator.IsRunning = false;
        PhotosLoadingIndicator.IsVisible = false;
        PhotosRetryButton.IsVisible = true;
        PhotosStateLabel.Text = message;
    }

    private void SetIdleState()
    {
        PhotosStatePanel.IsVisible = false;
        PhotosLoadingIndicator.IsRunning = false;
        PhotosLoadingIndicator.IsVisible = false;
        PhotosRetryButton.IsVisible = false;
    }
}
