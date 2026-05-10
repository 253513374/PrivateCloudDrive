using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class MediaAlbumsPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<MediaAlbumCard> AlbumCards { get; } = [];

    public string AlbumCountText => $"{AlbumCards.Count} 个相册";

    public string AlbumSummaryText => AlbumCards.Count == 0
        ? "整理照片、视频和项目素材"
        : $"{AlbumCards.Count} 个相册 · {AlbumCards.Sum(album => album.Album.ItemsCount)} 项媒体";

    public MediaAlbumsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAlbumsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadAlbumsAsync();
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("新建相册", "输入相册名称", "创建", "取消");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            await _apiClient.CreateMediaAlbumAsync(name.Trim());
            await LoadAlbumsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法创建相册", exception.Message, "确定");
        }
    }

    private async void OnAlbumSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MediaAlbumCard album)
        {
            return;
        }

        AlbumsCollectionView.SelectedItem = null;
        var route = $"media-album-detail?id={album.Id}&name={Uri.EscapeDataString(album.Name)}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async Task LoadAlbumsAsync()
    {
        SetLoadingState("正在加载相册...");

        try
        {
            var albums = await _apiClient.GetMediaAlbumsAsync();
            AlbumCards.Clear();
            foreach (var album in albums)
            {
                AlbumCards.Add(new MediaAlbumCard(album));
            }

            OnPropertyChanged(nameof(AlbumCountText));
            OnPropertyChanged(nameof(AlbumSummaryText));
            SetIdleState();
            await LoadAlbumCoversAsync();
        }
        catch (Exception exception)
        {
            AlbumCards.Clear();
            OnPropertyChanged(nameof(AlbumCountText));
            OnPropertyChanged(nameof(AlbumSummaryText));
            SetErrorState($"无法加载相册。{exception.Message}");
        }
    }

    private async Task LoadAlbumCoversAsync()
    {
        foreach (var album in AlbumCards)
        {
            if (!album.CoverFileNodeId.HasValue)
            {
                continue;
            }

            try
            {
                var content = await _apiClient.GetFileContentAsync(album.CoverFileNodeId.Value, thumbnail: true);
                album.CoverSource = ImageSource.FromStream(() => new MemoryStream(content.Content));
            }
            catch
            {
                // 相册封面缺失时保留占位图，不阻断列表浏览。
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
}
