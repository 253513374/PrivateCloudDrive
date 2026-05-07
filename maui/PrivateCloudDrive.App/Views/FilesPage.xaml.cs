using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class FilesPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly List<PathSegment> _path = [new(null, "Files")];
    private Guid? _currentFolderId;

    public ObservableCollection<CloudDriveItem> Items { get; } = [];

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    public string CurrentPath => string.Join(" / ", _path.Select(segment => segment.Name));

    public bool CanGoBack => _path.Count > 1;

    public FilesPage()
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

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        var files = await FilePicker.Default.PickMultipleAsync();
        if (files == null)
        {
            return;
        }

        UploadStatusPanel.IsVisible = true;

        try
        {
            foreach (var file in files.OfType<FileResult>())
            {
                UploadStatusLabel.Text = file.FileName;
                UploadProgressBar.Progress = 0;

                var progress = new Progress<double>(value =>
                {
                    UploadProgressBar.Progress = Math.Clamp(value, 0, 1);
                });

                await _apiClient.UploadFileAsync(_currentFolderId, file, progress);
            }

            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Upload failed", exception.Message, "OK");
        }
        finally
        {
            UploadStatusPanel.IsVisible = false;
            UploadProgressBar.Progress = 0;
        }
    }

    private async void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CloudDriveItem item)
        {
            return;
        }

        FilesCollectionView.SelectedItem = null;

        if (item.IsFolder)
        {
            _currentFolderId = item.Id;
            _path.Add(new PathSegment(item.Id, item.Name));
            NotifyNavigationChanged();
            await LoadItemsAsync();
            return;
        }

        if (!item.CanPreview)
        {
            return;
        }

        var route = $"media-preview?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={Uri.EscapeDataString(item.Kind)}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_path.Count <= 1)
        {
            return;
        }

        _path.RemoveAt(_path.Count - 1);
        _currentFolderId = _path[^1].Id;
        NotifyNavigationChanged();
        await LoadItemsAsync();
    }

    private async void OnNewFolderClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            "New folder",
            "Folder name",
            accept: "Create",
            cancel: "Cancel",
            maxLength: 128,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            await _apiClient.CreateFolderAsync(_currentFolderId, name.Trim());
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Unable to create folder", exception.Message, "OK");
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await _authService.SignOutAsync();
        await Shell.Current.GoToAsync("//login", true);
    }

    private async Task LoadItemsAsync()
    {
        RefreshButton.IsEnabled = false;

        try
        {
            var items = await _apiClient.GetItemsAsync(_currentFolderId);
            Items.Clear();

            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Unable to load files", exception.Message, "OK");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void NotifyNavigationChanged()
    {
        OnPropertyChanged(nameof(CurrentPath));
        OnPropertyChanged(nameof(CanGoBack));
    }

    private sealed record PathSegment(Guid? Id, string Name);
}
