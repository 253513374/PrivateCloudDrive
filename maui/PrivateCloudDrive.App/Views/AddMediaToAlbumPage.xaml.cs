using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

[QueryProperty(nameof(AlbumId), "id")]
[QueryProperty(nameof(AlbumName), "name")]
public partial class AddMediaToAlbumPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly List<SelectableMediaLibraryItem> _allItems = [];
    private string? _selectedMediaType;

    public ObservableCollection<SelectableMediaLibraryItem> Items { get; } = [];

    public string AlbumId { get; set; } = string.Empty;

    public string AlbumName { get; set; } = "相册";

    public string HeaderText => $"{AlbumName} · {Items.Count} 项可添加";

    public string SelectionSummaryText => $"{Items.Count} 项可添加 · 已选 {SelectedCount} 项";

    public string AddButtonText => SelectedCount == 0 ? "加入" : $"加入 {SelectedCount}";

    public bool CanAddSelected => SelectedCount > 0;

    private int SelectedCount => _allItems.Count(item => item.IsSelected);

    public AddMediaToAlbumPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAvailableMediaAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadAvailableMediaAsync();
    }

    private void OnAllMediaClicked(object? sender, EventArgs e)
    {
        _selectedMediaType = null;
        ApplyCurrentFilter();
    }

    private void OnImagesClicked(object? sender, EventArgs e)
    {
        _selectedMediaType = "Image";
        ApplyCurrentFilter();
    }

    private void OnVideosClicked(object? sender, EventArgs e)
    {
        _selectedMediaType = "Video";
        ApplyCurrentFilter();
    }

    private void OnMediaSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }

        if (e.CurrentSelection.FirstOrDefault() is not SelectableMediaLibraryItem item)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
        NotifySelectionChanged();
    }

    private async void OnAddSelectedClicked(object? sender, EventArgs e)
    {
        if (!TryGetAlbumId(out var albumId))
        {
            return;
        }

        var selectedIds = _allItems
            .Where(item => item.IsSelected)
            .Select(item => item.Id)
            .ToList();

        if (selectedIds.Count == 0)
        {
            return;
        }

        try
        {
            await _apiClient.AddMediaAlbumItemsAsync(albumId, selectedIds);
            await Shell.Current.GoToAsync("..", true);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法加入相册", exception.Message, "确定");
        }
    }

    private async Task LoadAvailableMediaAsync()
    {
        if (!TryGetAlbumId(out var albumId))
        {
            SetErrorState("相册 ID 无效。");
            return;
        }

        RefreshButton.IsEnabled = false;
        SetLoadingState("正在加载可添加媒体...");

        try
        {
            var existingIds = (await _apiClient.GetMediaAlbumItemsAsync(albumId, maxResultCount: 500))
                .Select(item => item.Id)
                .ToHashSet();

            var media = await _apiClient.GetMediaTimelineAsync(maxResultCount: 500);
            _allItems.Clear();

            foreach (var item in media.Where(item => !existingIds.Contains(item.Id)))
            {
                _allItems.Add(new SelectableMediaLibraryItem(new MediaLibraryItem(item)));
            }

            ApplyCurrentFilter();
            SetIdleState();
            await LoadThumbnailsAsync();
        }
        catch (Exception exception)
        {
            _allItems.Clear();
            Items.Clear();
            NotifySelectionChanged();
            SetErrorState($"无法加载可添加媒体。{exception.Message}");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void ApplyCurrentFilter()
    {
        Items.Clear();

        var items = _selectedMediaType switch
        {
            "Image" => _allItems.Where(item => !item.IsVideo),
            "Video" => _allItems.Where(item => item.IsVideo),
            _ => _allItems
        };

        foreach (var item in items)
        {
            Items.Add(item);
        }

        UpdateSegmentButtons();
        NotifySelectionChanged();
    }

    private async Task LoadThumbnailsAsync()
    {
        foreach (var item in _allItems)
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
                // Keep the media type badge visible while thumbnails are missing or processing.
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

    private void UpdateSegmentButtons()
    {
        SetSegmentButton(AllMediaButton, _selectedMediaType is null);
        SetSegmentButton(ImagesButton, _selectedMediaType == "Image");
        SetSegmentButton(VideosButton, _selectedMediaType == "Video");
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

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(SelectionSummaryText));
        OnPropertyChanged(nameof(AddButtonText));
        OnPropertyChanged(nameof(CanAddSelected));
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
