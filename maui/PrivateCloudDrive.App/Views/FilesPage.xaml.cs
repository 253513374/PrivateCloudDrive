using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;
#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示FilesPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class FilesPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IUploadQueueService _uploadQueueService = AppServices.GetRequiredService<IUploadQueueService>();
    private readonly List<PathSegment> _path = [new(null, AppText.Files)];
    private Guid? _currentFolderId;
    private bool _filtersInitialized;
    private bool _isSelectionMode;

    public ObservableCollection<CloudDriveItem> Items { get; } = [];

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    public string CurrentPath => string.Join(" / ", _path.Select(segment => segment.Name));

    public bool CanGoBack => _path.Count > 1;

    public bool ShowCurrentPath => CanGoBack;

    public string ItemCountText => Items.Count == 0
        ? "暂无项目"
        : $"{Items.Count} 个项目";

    /// <summary>
    /// 初始化 <see cref="FilesPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FilesPage()
    {
        InitializeComponent();
        BindingContext = this;
        InitializeFilterPickers();
        UploadItemsSubscribe();
        UpdateUploadTaskPanel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateUploadTaskPanel();
        await LoadItemsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnSearchPressed(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.OldTextValue) || !string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            return;
        }

        await LoadItemsAsync();
    }

    private async void OnFilterChanged(object? sender, EventArgs e)
    {
        if (!_filtersInitialized)
        {
            return;
        }

        await LoadItemsAsync();
    }

    private async void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        _filtersInitialized = false;
        FilesSearchBar.Text = string.Empty;
        SearchAllSwitch.IsToggled = false;
        SortPicker.SelectedIndex = 0;
        TypeFilterPicker.SelectedIndex = 0;
        MediaFilterPicker.SelectedIndex = 0;
        _filtersInitialized = true;
        await LoadItemsAsync();
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        try
        {
            var files = await PickUploadFilesAsync();
            if (files.Count == 0)
            {
                return;
            }

            UploadStatusPanel.IsVisible = true;
            UpdateUploadTaskPanel();

            var failedUploads = new List<string>();

            foreach (var file in files)
            {
                UploadStatusLabel.Text = file.FileName;
                UploadProgressBar.Progress = 0;
                var queueItem = _uploadQueueService.Enqueue(file, CurrentPath);
                queueItem.MarkUploading();
                UpdateUploadTaskPanel();

                var progress = new Progress<double>(value =>
                {
                    UploadProgressBar.Progress = Math.Clamp(value, 0, 1);
                    queueItem.UpdateProgress(value);
                });

                try
                {
                    await _apiClient.UploadFileAsync(_currentFolderId, file, progress);
                    queueItem.MarkCompleted();
                }
                catch (Exception exception)
                {
                    var message = await WriteUploadErrorAsync(exception);
                    queueItem.MarkFailed(message);
                    failedUploads.Add($"{file.FileName}: {message}");
                }
            }

            await LoadItemsAsync();

            if (failedUploads.Count > 0)
            {
                await DisplayAlertAsync(
                    AppText.SomeUploadsFailed,
                    string.Join(Environment.NewLine, failedUploads),
                    "OK");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            var message = await WriteUploadErrorAsync(exception);
            await DisplayAlertAsync(AppText.UploadFailed, message, "OK");
        }
        finally
        {
            UpdateUploadTaskPanel();
        }
    }

    private async void OnOpenUploadsClicked(object? sender, EventArgs e)
    {
        await OpenUploadsAsync();
    }

    private async void OnOpenUploadsTapped(object? sender, TappedEventArgs e)
    {
        await OpenUploadsAsync();
    }

    private static Task OpenUploadsAsync()
    {
        return Shell.Current.GoToAsync("uploads", true);
    }

    private void UploadItemsSubscribe()
    {
        _uploadQueueService.Items.CollectionChanged += OnUploadItemsChanged;

        foreach (var item in _uploadQueueService.Items)
        {
            item.PropertyChanged += OnUploadItemPropertyChanged;
        }
    }

    private void OnUploadItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (UploadQueueItem item in e.OldItems)
            {
                item.PropertyChanged -= OnUploadItemPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (UploadQueueItem item in e.NewItems)
            {
                item.PropertyChanged += OnUploadItemPropertyChanged;
            }
        }

        UpdateUploadTaskPanel();
    }

    private void OnUploadItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UploadQueueItem.Status) or nameof(UploadQueueItem.Progress) or nameof(UploadQueueItem.ErrorMessage))
        {
            UpdateUploadTaskPanel();
        }
    }

    private void UpdateUploadTaskPanel()
    {
        var items = _uploadQueueService.Items;
        if (items.Count == 0)
        {
            UploadStatusPanel.IsVisible = false;
            UploadStatusLabel.Text = string.Empty;
            UploadProgressBar.Progress = 0;
            return;
        }

        var waiting = items.Count(item => item.Status == UploadQueueStatus.Waiting);
        var uploading = items.Count(item => item.Status == UploadQueueStatus.Uploading);
        var failed = items.Count(item => item.Status == UploadQueueStatus.Failed);
        var completed = items.Count(item => item.Status == UploadQueueStatus.Completed);
        var active = waiting + uploading;

        UploadStatusPanel.IsVisible = true;

        if (failed > 0)
        {
            UploadStatusLabel.Text = failed == 1
                ? "1 个文件上传失败，点击查看并重试"
                : $"{failed} 个文件上传失败，点击查看并重试";
            UploadProgressBar.Progress = 0;
            return;
        }

        if (active > 0)
        {
            var activeItems = items.Where(item => item.Status is UploadQueueStatus.Waiting or UploadQueueStatus.Uploading).ToList();
            var current = activeItems.FirstOrDefault(item => item.Status == UploadQueueStatus.Uploading) ?? activeItems[0];
            var averageProgress = activeItems.Count == 0 ? 0 : activeItems.Average(item => item.Progress);

            UploadStatusLabel.Text = active == 1
                ? $"正在上传 {current.FileName} · {current.Progress:P0}"
                : $"正在上传 {uploading} 个 · 等待 {waiting} 个 · {averageProgress:P0}";
            UploadProgressBar.Progress = averageProgress;
            return;
        }

        UploadStatusLabel.Text = completed == 1
            ? "已完成 1 个上传，点击查看记录"
            : $"已完成 {completed} 个上传，点击查看记录";
        UploadProgressBar.Progress = 1;
    }

    private async void OnRetryLoadClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private static async Task<IReadOnlyList<FileResult>> PickUploadFilesAsync()
    {
#if WINDOWS
        var paths = NativeFileDialog.PickFiles();

        IReadOnlyList<FileResult> files = paths
            .Select(path => new FileResult(path, GetContentType(Path.GetExtension(path))))
            .ToList();

        return files;
#else
        var pickedFiles = await FilePicker.Default.PickMultipleAsync();
        return pickedFiles?.OfType<FileResult>().ToList() ?? [];
#endif
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".m4v" => "video/x-m4v",
            ".webm" => "video/webm",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private static async Task<string> WriteUploadErrorAsync(Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? AppText.Format(nameof(AppText.UploadFailedBeforeRequest), exception.GetType().Name)
            : exception.Message;

        try
        {
            var logPath = Path.Combine(FileSystem.AppDataDirectory, "upload-errors.log");
            await File.AppendAllTextAsync(
                logPath,
                $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // The UI message is more important than diagnostic logging.
        }

        return message;
    }

#if WINDOWS
    private static class NativeFileDialog
    {
        private const int BufferCharCount = 65536;
        private const int OfnAllowMultiSelect = 0x00000200;
        private const int OfnExplorer = 0x00080000;
        private const int OfnFileMustExist = 0x00001000;
        private const int OfnPathMustExist = 0x00000800;
        private const int OfnNoChangeDir = 0x00000008;

        /// <summary>
        /// 执行PickFiles操作，封装该场景下的业务规则、异常处理和结果返回。
        /// </summary>
        public static IReadOnlyList<string> PickFiles()
        {
            var buffer = Marshal.AllocHGlobal(BufferCharCount * sizeof(char));

            try
            {
                Marshal.WriteInt16(buffer, 0);

                var openFileName = new OpenFileName
                {
                    StructSize = Marshal.SizeOf<OpenFileName>(),
                    File = buffer,
                    MaxFile = BufferCharCount,
                    Filter = $"{AppText.AllFilesFilter}\0*.*\0\0",
                    FilterIndex = 1,
                    Title = AppText.SelectFilesToUpload,
                    Flags = OfnExplorer | OfnAllowMultiSelect | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir
                };

                if (!GetOpenFileName(openFileName))
                {
                    var error = CommDlgExtendedError();
                    if (error == 0)
                    {
                        return [];
                    }

                    throw new InvalidOperationException(AppText.Format(nameof(AppText.WindowsFileDialogFailed), error.ToString("X")));
                }

                return ParseSelectedFiles(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static IReadOnlyList<string> ParseSelectedFiles(IntPtr buffer)
        {
            var values = new List<string>();
            var current = new List<char>();

            for (var index = 0; index < BufferCharCount; index++)
            {
                var value = (char)Marshal.ReadInt16(buffer, index * sizeof(char));
                if (value == '\0')
                {
                    if (current.Count == 0)
                    {
                        break;
                    }

                    values.Add(new string(current.ToArray()));
                    current.Clear();
                    continue;
                }

                current.Add(value);
            }

            if (values.Count <= 1)
            {
                return values;
            }

            var directory = values[0];
            return values
                .Skip(1)
                .Select(fileName => Path.Combine(directory, fileName))
                .ToList();
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName openFileName);

        [DllImport("comdlg32.dll")]
        private static extern int CommDlgExtendedError();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class OpenFileName
        {
            public int StructSize;

            public IntPtr Owner;

            public IntPtr Instance;

            public string? Filter;

            public IntPtr CustomFilter;

            public int MaxCustomFilter;

            public int FilterIndex;

            public IntPtr File;

            public int MaxFile;

            public IntPtr FileTitle;

            public int MaxFileTitle;

            public string? InitialDirectory;

            public string? Title;

            public int Flags;

            public short FileOffset;

            public short FileExtension;

            public string? DefaultExtension;

            public IntPtr CustomData;

            public IntPtr Hook;

            public string? TemplateName;

            public IntPtr Reserved;

            public int ReservedValue;

            public int FlagsEx;
        }
    }
#endif

    private async void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSelectionMode)
        {
            UpdateSelectedItems();
            return;
        }

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

        var route = item.CanPreview
            ? $"media-preview?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={Uri.EscapeDataString(item.Kind)}"
            : $"file-details?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={Uri.EscapeDataString(item.Kind)}&size={Uri.EscapeDataString(item.Size)}&modified={Uri.EscapeDataString(item.ModifiedAt)}&favorite={item.IsFavorite}";

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
            AppText.NewFolderLower,
            AppText.FolderName,
            accept: AppText.Create,
            cancel: AppText.Cancel,
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
            await DisplayAlertAsync(AppText.UnableToCreateFolder, exception.Message, "OK");
        }
    }

    private async void OnDeleteItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.MoveToTrash,
            AppText.Format(nameof(AppText.MoveToTrashQuestion), item.Name),
            AppText.Move,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.DeleteItemAsync(item.Id);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync(AppText.UnableToDelete, exception.Message, "OK");
        }
    }

    private void OnToggleSelectionModeClicked(object? sender, EventArgs e)
    {
        SetSelectionMode(!_isSelectionMode);
    }

    private async void OnBatchDeleteClicked(object? sender, EventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            await DisplayAlertAsync("批量操作", "请先选择文件或文件夹。", "OK");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.MoveToTrash,
            $"将 {selectedItems.Count} 项移入回收站？",
            AppText.Move,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.DeleteItemsAsync(selectedItems.Select(item => item.Id).ToList());
            SetSelectionMode(false);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync(AppText.UnableToDelete, exception.Message, "OK");
        }
    }

    private async void OnBatchFavoriteClicked(object? sender, EventArgs e)
    {
        await SetSelectedFavoriteAsync(isFavorite: true);
    }

    private async void OnBatchUnfavoriteClicked(object? sender, EventArgs e)
    {
        await SetSelectedFavoriteAsync(isFavorite: false);
    }

    private async void OnBatchMoveRootClicked(object? sender, EventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            await DisplayAlertAsync("批量操作", "请先选择文件或文件夹。", "OK");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "移动到根目录",
            $"将 {selectedItems.Count} 项移动到根目录？",
            AppText.Move,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.MoveItemsAsync(selectedItems.Select(item => item.Id).ToList(), parentId: null);
            SetSelectionMode(false);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法移动", exception.Message, "OK");
        }
    }

    private async void OnDetailsItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        var route = $"file-details?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={Uri.EscapeDataString(item.Kind)}&size={Uri.EscapeDataString(item.Size)}&modified={Uri.EscapeDataString(item.ModifiedAt)}&favorite={item.IsFavorite}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await _authService.SignOutAsync();
        await Shell.Current.GoToAsync("//login", true);
    }

    private async Task LoadItemsAsync()
    {
        RefreshButton.IsEnabled = false;
        SetFilesLoadingState(AppText.LoadingFiles);

        try
        {
            var items = await _apiClient.GetItemsAsync(_currentFolderId, options: CreateQueryOptions());
            Items.Clear();

            foreach (var item in items)
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(ItemCountText));
            SetFilesIdleState();
        }
        catch (AuthSessionExpiredException)
        {
            Items.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            Items.Clear();
            OnPropertyChanged(nameof(ItemCountText));
            SetFilesErrorState(AppText.Format(nameof(AppText.UnableToLoadFiles), exception.Message));
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void SetFilesLoadingState(string message)
    {
        FilesStatePanel.IsVisible = true;
        FilesLoadingIndicator.IsVisible = true;
        FilesLoadingIndicator.IsRunning = true;
        FilesRetryButton.IsVisible = false;
        FilesStateLabel.Text = message;
    }

    private void SetFilesErrorState(string message)
    {
        FilesStatePanel.IsVisible = true;
        FilesLoadingIndicator.IsRunning = false;
        FilesLoadingIndicator.IsVisible = false;
        FilesRetryButton.IsVisible = true;
        FilesStateLabel.Text = message;
    }

    private void SetFilesIdleState()
    {
        FilesStatePanel.IsVisible = false;
        FilesLoadingIndicator.IsRunning = false;
        FilesLoadingIndicator.IsVisible = false;
        FilesRetryButton.IsVisible = false;
    }

    private void NotifyNavigationChanged()
    {
        OnPropertyChanged(nameof(CurrentPath));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(ShowCurrentPath));
    }

    private void InitializeFilterPickers()
    {
        SortPicker.ItemsSource = new List<string>
        {
            "名称 A-Z",
            "名称 Z-A",
            "大小从小到大",
            "大小从大到小",
            "最新创建",
            "最早创建",
            "最近修改"
        };
        TypeFilterPicker.ItemsSource = new List<string> { "全部类型", "文件夹", "文件" };
        MediaFilterPicker.ItemsSource = new List<string> { "全部媒体", "图片", "视频", "其他文件" };

        SortPicker.SelectedIndex = 0;
        TypeFilterPicker.SelectedIndex = 0;
        MediaFilterPicker.SelectedIndex = 0;
        _filtersInitialized = true;
    }

    private CloudDriveQueryOptions CreateQueryOptions()
    {
        return new CloudDriveQueryOptions
        {
            SearchKeyword = string.IsNullOrWhiteSpace(FilesSearchBar.Text) ? null : FilesSearchBar.Text.Trim(),
            SearchScope = SearchAllSwitch.IsToggled ? "All" : "CurrentFolder",
            NodeType = TypeFilterPicker.SelectedIndex switch
            {
                1 => "Folder",
                2 => "File",
                _ => null
            },
            MediaType = MediaFilterPicker.SelectedIndex switch
            {
                1 => "Image",
                2 => "Video",
                3 => "Other",
                _ => null
            },
            Sorting = SortPicker.SelectedIndex switch
            {
                1 => "name desc",
                2 => "size asc",
                3 => "size desc",
                4 => "creationTime desc",
                5 => "creationTime asc",
                6 => "lastModificationTime desc",
                _ => null
            }
        };
    }

    private async Task SetSelectedFavoriteAsync(bool isFavorite)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            await DisplayAlertAsync("批量操作", "请先选择文件或文件夹。", "OK");
            return;
        }

        try
        {
            await _apiClient.SetFavoriteItemsAsync(selectedItems.Select(item => item.Id).ToList(), isFavorite);
            SetSelectionMode(false);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("无法更新收藏", exception.Message, "OK");
        }
    }

    private void SetSelectionMode(bool isSelectionMode)
    {
        _isSelectionMode = isSelectionMode;
        SelectionModeButton.Text = _isSelectionMode ? "完成" : "选择";
        BatchToolbar.IsVisible = _isSelectionMode;
        FilesCollectionView.SelectionMode = _isSelectionMode
            ? SelectionMode.Multiple
            : SelectionMode.Single;

        FilesCollectionView.SelectedItems.Clear();
        UpdateSelectedItems();
    }

    private IReadOnlyList<CloudDriveItem> GetSelectedItems()
    {
        return FilesCollectionView.SelectedItems
            .OfType<CloudDriveItem>()
            .ToList();
    }

    private void UpdateSelectedItems()
    {
        var count = GetSelectedItems().Count;
        SelectedCountLabel.Text = $"已选择 {count} 项";
        BatchMoveRootButton.IsEnabled = count > 0 && _currentFolderId.HasValue;
    }

    private sealed record PathSegment(Guid? Id, string Name);
}
