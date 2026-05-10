using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

[QueryProperty(nameof(AlbumId), "id")]
[QueryProperty(nameof(AlbumName), "name")]
public partial class MediaAlbumDetailPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<MediaLibraryItem> Items { get; } = [];

    public string AlbumId { get; set; } = string.Empty;

    public string AlbumName { get; set; } = "相册";

    public string ItemCountText => $"{Items.Count} 项媒体";

    public MediaAlbumDetailPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(AlbumName));
        await LoadItemsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnAddMediaClicked(object? sender, EventArgs e)
    {
        if (!TryGetAlbumId(out var albumId))
        {
            return;
        }

        var route = $"media-album-add?id={albumId}&name={Uri.EscapeDataString(AlbumName)}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async void OnDeleteAlbumClicked(object? sender, EventArgs e)
    {
        if (!TryGetAlbumId(out var albumId))
        {
            return;
        }

        var confirmed = await DisplayAlertAsync("删除相册", $"删除“{AlbumName}”？原文件不会被删除。", "删除", "取消");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.DeleteMediaAlbumAsync(albumId);
            await Shell.Current.GoToAsync("..", true);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法删除相册", exception.Message, "确定");
        }
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

    private async void OnSetCoverClicked(object? sender, EventArgs e)
    {
        if (!TryGetAlbumId(out var albumId) || sender is not Button button || button.CommandParameter is not Guid fileNodeId)
        {
            return;
        }

        try
        {
            await _apiClient.SetMediaAlbumCoverAsync(albumId, fileNodeId);
            await DisplayAlertAsync("已设置封面", "相册封面已更新。", "确定");
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法设置封面", exception.Message, "确定");
        }
    }

    private async void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (!TryGetAlbumId(out var albumId) || sender is not Button button || button.CommandParameter is not Guid fileNodeId)
        {
            return;
        }

        try
        {
            await _apiClient.RemoveMediaAlbumItemAsync(albumId, fileNodeId);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法移除媒体", exception.Message, "确定");
        }
    }

    private async Task LoadItemsAsync()
    {
        if (!TryGetAlbumId(out var albumId))
        {
            SetErrorState("相册 ID 无效。");
            return;
        }

        SetLoadingState("正在加载相册媒体...");

        try
        {
            var items = await _apiClient.GetMediaAlbumItemsAsync(albumId);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new MediaLibraryItem(item));
            }

            OnPropertyChanged(nameof(ItemCountText));
            SetIdleState();
            await LoadThumbnailsAsync();
        }
        catch (Exception exception)
        {
            Items.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            SetErrorState($"无法加载相册媒体。{exception.Message}");
        }
    }

    private async Task LoadThumbnailsAsync()
    {
        foreach (var item in Items)
        {
            try
            {
                var content = item.IsVideo
                    ? await _apiClient.GetFileContentAsync(item.Id, thumbnail: true)
                    : await GetThumbnailOrImageAsync(item.Id);
                item.ThumbnailSource = ImageSource.FromStream(() => new MemoryStream(content.Content));
            }
            catch
            {
                // Keep the badge fallback visible while thumbnails are missing or processing.
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

    private bool TryGetAlbumId(out Guid albumId)
    {
        return Guid.TryParse(AlbumId, out albumId);
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
}
