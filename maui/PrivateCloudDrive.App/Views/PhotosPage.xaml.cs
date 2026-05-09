using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示PhotosPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class PhotosPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly List<MediaLibraryItem> _items = [];
    private string? _selectedMediaType;

    public ObservableCollection<MediaTimelineGroup> TimelineGroups { get; } = [];

    public string ItemCountText => $"{_items.Count} 项媒体";

    /// <summary>
    /// 初始化 <see cref="PhotosPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public PhotosPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMediaAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadMediaAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadMediaAsync();
    }

    private async void OnPhotoSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MediaLibraryItem item)
        {
            return;
        }

        PhotosCollectionView.SelectedItem = null;
        var route = $"media-preview?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={item.Kind}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async void OnAllMediaClicked(object? sender, EventArgs e)
    {
        _selectedMediaType = null;
        await LoadMediaAsync();
    }

    private async void OnImagesClicked(object? sender, EventArgs e)
    {
        _selectedMediaType = "Image";
        await LoadMediaAsync();
    }

    private async void OnVideosClicked(object? sender, EventArgs e)
    {
        _selectedMediaType = "Video";
        await LoadMediaAsync();
    }

    private async void OnAlbumsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("media-albums", true);
    }

    private async void OnProcessingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("media-processing", true);
    }

    private async Task LoadMediaAsync()
    {
        RefreshButton.IsEnabled = false;
        SetLoadingState("正在加载媒体...");

        try
        {
            var media = await _apiClient.GetMediaTimelineAsync(mediaType: _selectedMediaType);
            _items.Clear();

            foreach (var item in media)
            {
                _items.Add(new MediaLibraryItem(item));
            }

            RebuildGroups();
            OnPropertyChanged(nameof(ItemCountText));
            SetIdleState();
            await LoadThumbnailsAsync();
        }
        catch (Exception exception)
        {
            _items.Clear();
            TimelineGroups.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            SetErrorState($"无法加载媒体。{exception.Message}");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void RebuildGroups()
    {
        TimelineGroups.Clear();

        var groups = _items
            .GroupBy(item =>
            {
                var timelineTime = item.TimelineItem?.TimelineTime ?? DateTime.Now;
                return new DateTime(timelineTime.Year, timelineTime.Month, 1);
            })
            .OrderByDescending(group => group.Key);

        foreach (var group in groups)
        {
            TimelineGroups.Add(new MediaTimelineGroup(
                group.Key,
                group.OrderByDescending(item => item.TimelineItem?.TimelineTime ?? DateTime.MinValue)));
        }
    }

    private async Task LoadThumbnailsAsync()
    {
        foreach (var item in _items)
        {
            try
            {
                var content = item.IsVideo
                    ? await _apiClient.GetFileContentAsync(item.Id, thumbnail: true)
                    : await GetThumbnailOrImageAsync(item.Id);
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
