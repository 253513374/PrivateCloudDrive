using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示VideosPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class VideosPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<MediaLibraryItem> Items { get; } = [];

    public string ItemCountText => AppText.Format(nameof(AppText.VideosCount), Items.Count);

    /// <summary>
    /// 初始化 <see cref="VideosPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public VideosPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadVideosAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadVideosAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadVideosAsync();
    }

    private async void OnVideoSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MediaLibraryItem item)
        {
            return;
        }

        VideosCollectionView.SelectedItem = null;
        var route = $"media-preview?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind=Video";
        await Shell.Current.GoToAsync(route, true);
    }

    private async Task LoadVideosAsync()
    {
        RefreshButton.IsEnabled = false;
        SetLoadingState(AppText.LoadingVideos);

        try
        {
            var videos = await _apiClient.GetVideosAsync();
            Items.Clear();

            foreach (var video in videos)
            {
                Items.Add(new MediaLibraryItem(video));
            }

            OnPropertyChanged(nameof(ItemCountText));
            SetIdleState();
            await LoadThumbnailsAsync();
        }
        catch (Exception exception)
        {
            Items.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            SetErrorState(AppText.Format(nameof(AppText.UnableToLoadVideos), exception.Message));
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
                var content = await _apiClient.GetFileContentAsync(item.Id, thumbnail: true);
                var bytes = content.Content;
                item.ThumbnailSource = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                // Keep the badge fallback visible when thumbnail processing is not ready.
            }
        }
    }

    private void SetLoadingState(string message)
    {
        VideosStatePanel.IsVisible = true;
        VideosLoadingIndicator.IsVisible = true;
        VideosLoadingIndicator.IsRunning = true;
        VideosRetryButton.IsVisible = false;
        VideosStateLabel.Text = message;
    }

    private void SetErrorState(string message)
    {
        VideosStatePanel.IsVisible = true;
        VideosLoadingIndicator.IsRunning = false;
        VideosLoadingIndicator.IsVisible = false;
        VideosRetryButton.IsVisible = true;
        VideosStateLabel.Text = message;
    }

    private void SetIdleState()
    {
        VideosStatePanel.IsVisible = false;
        VideosLoadingIndicator.IsRunning = false;
        VideosLoadingIndicator.IsVisible = false;
        VideosRetryButton.IsVisible = false;
    }
}
