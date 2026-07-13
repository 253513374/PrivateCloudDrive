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
    private readonly IBackupTransferService _backupTransferService = AppServices.GetRequiredService<IBackupTransferService>();
    private readonly List<PathSegment> _path = [new(null, AppText.Files)];
    private Guid? _currentFolderId;
    private bool _filtersInitialized;
    private bool _isSelectionMode;

    // ---- 排序与筛选状态 ----
    private int _sortOption;        // 0=名称A-Z, 1=名称Z-A, 2=时间新→旧, 3=时间旧→新, 4=大小大→小, 5=大小小→大, 6=按类型分组
    private int _filterOption;      // 0=全部, 1=仅文件, 2=仅文件夹
    private bool _isFavoriteFilter;
    private Guid? _selectedTagId;
    private IReadOnlyList<CloudDriveTag> _tagsCache = [];

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
        InitializeFilterState();
        UploadItemsSubscribe();
        UpdateUploadTaskPanel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateUploadTaskPanel();
        await LoadStorageUsageAsync();
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

    private async void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        _filtersInitialized = false;
        FilesSearchBar.Text = string.Empty;
        SearchAllSwitch.IsToggled = false;
        _sortOption = 0;
        _filterOption = 0;
        _isFavoriteFilter = false;
        _selectedTagId = null;
        UpdateChipVisualStates();
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

            var queueItems = await _backupTransferService.BackupFilesAsync(_currentFolderId, CurrentPath, files);
            await LoadItemsAsync();

            var failedUploads = queueItems
                .Where(item => item.IsFailed)
                .Select(item => $"{item.FileName}: {item.ErrorMessage}")
                .ToList();

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
            await DisplayAlertAsync(AppText.UploadFailed, exception.Message, "OK");
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
        return Shell.Current.GoToAsync("//main/uploads", true);
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
                ? "1 个文件备份失败，点击查看并重试"
                : $"{failed} 个文件备份失败，点击查看并重试";
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

    private async Task LoadStorageUsageAsync()
    {
        try
        {
            var isSignedIn = await _authService.IsSignedInAsync();
            if (!isSignedIn)
            {
                SetFilesStorageSignedOutState();
                return;
            }

            var usage = await _apiClient.GetStorageUsageAsync();
            SetFilesStorageUsageState(usage);
        }
        catch (AuthSessionExpiredException)
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception)
        {
            SetFilesStorageDegradedState();
        }
    }

    private void SetFilesStorageUsageState(StorageUsage usage)
    {
        if (usage.IsQuotaConfigured)
        {
            var percent = Math.Clamp((double)usage.UsagePercent, 0, 100);
            FilesCapacityLabel.Text = $"{percent:0.#}%";
            FilesStorageSubLabel.Text = $"{FormatBytes(usage.UsedBytes)} / {FormatBytes(usage.QuotaBytes)}";
            FilesStorageProgressBar.Progress = percent / 100;
            var detailParts = new List<string>
            {
                $"剩余 {FormatBytes(usage.RemainingBytes)}"
            };
            if (usage.MaxSingleFileSize > 0)
            {
                detailParts.Add($"单文件上限 {FormatBytes(usage.MaxSingleFileSize)}");
            }
            FilesStorageDetailLabel.Text = string.Join(" · ", detailParts);
        }
        else
        {
            FilesCapacityLabel.Text = "无限";
            FilesStorageSubLabel.Text = $"{FormatBytes(usage.UsedBytes)} 已备份";
            FilesStorageProgressBar.Progress = 0;
            var detailParts = new List<string>
            {
                "未配置容量上限"
            };
            if (usage.MaxSingleFileSize > 0)
            {
                detailParts.Add($"单文件上限 {FormatBytes(usage.MaxSingleFileSize)}");
            }
            FilesStorageDetailLabel.Text = string.Join(" · ", detailParts);
        }
    }

    private void SetFilesStorageSignedOutState()
    {
        FilesCapacityLabel.Text = "--";
        FilesStorageSubLabel.Text = "登录后可查看容量";
        FilesStorageProgressBar.Progress = 0;
        FilesStorageDetailLabel.Text = string.Empty;
    }

    private void SetFilesStorageDegradedState()
    {
        FilesCapacityLabel.Text = "降级";
        FilesCapacityLabel.TextColor = Colors.Orange;
        FilesStorageSubLabel.Text = "容量暂时不可用";
        FilesStorageProgressBar.Progress = 0;
        FilesStorageDetailLabel.Text = "请在\"我的\"页重试";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(bytes, 0);
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }

    private void InitializeFilterState()
    {
        _sortOption = 0;
        _filterOption = 0;
        _isFavoriteFilter = false;
        _selectedTagId = null;
        _tagsCache = [];
        UpdateChipVisualStates();
        _filtersInitialized = true;
    }

    private async void OnSortChipTapped(object? sender, TappedEventArgs e)
    {
        var options = new[]
        {
            AppText.SortNameAsc,
            AppText.SortNameDesc,
            AppText.SortTimeNew,
            AppText.SortTimeOld,
            AppText.SortSizeDesc,
            AppText.SortSizeAsc,
            AppText.SortByType
        };

        var selected = await DisplayActionSheetAsync(AppText.Sort, AppText.Cancel, null, options);
        if (string.IsNullOrEmpty(selected) || selected == AppText.Cancel)
        {
            return;
        }

        var newIndex = Array.IndexOf(options, selected);
        if (newIndex < 0)
        {
            return;
        }

        _sortOption = newIndex;
        UpdateChipVisualStates();
        await LoadItemsAsync();
    }

    private async void OnFilterTypeChipTapped(object? sender, TappedEventArgs e)
    {
        var options = new[]
        {
            AppText.FilterAll,
            AppText.FilterFilesOnly,
            AppText.FilterFoldersOnly
        };

        var selected = await DisplayActionSheetAsync(AppText.Filter, AppText.Cancel, null, options);
        if (string.IsNullOrEmpty(selected) || selected == AppText.Cancel)
        {
            return;
        }

        var newIndex = Array.IndexOf(options, selected);
        if (newIndex < 0)
        {
            return;
        }

        _filterOption = newIndex;
        UpdateChipVisualStates();
        await LoadItemsAsync();
    }

    private async void OnFavChipTapped(object? sender, TappedEventArgs e)
    {
        _isFavoriteFilter = !_isFavoriteFilter;
        if (_isFavoriteFilter)
        {
            _selectedTagId = null;
        }

        UpdateChipVisualStates();
        await LoadItemsAsync();
    }

    private async void OnTagsChipTapped(object? sender, TappedEventArgs e)
    {
        // 确保标签缓存已加载
        if (_tagsCache.Count == 0)
        {
            try
            {
                _tagsCache = await _apiClient.GetTagsAsync();
            }
            catch
            {
                // 静默失败，展示空列表
            }
        }

        if (_tagsCache.Count == 0)
        {
            await DisplayAlertAsync(AppText.FilterTags, AppText.NoTagsAvailable, "OK");
            return;
        }

        var options = new List<string> { AppText.AllTags };
        options.AddRange(_tagsCache.Select(t => t.Name));

        // 当前已选标签名称
        var selected = await DisplayActionSheetAsync(AppText.FilterTags, AppText.Cancel, null, options.ToArray());
        if (string.IsNullOrEmpty(selected) || selected == AppText.Cancel)
        {
            return;
        }

        if (selected == AppText.AllTags)
        {
            _selectedTagId = null;
        }
        else
        {
            var tag = _tagsCache.FirstOrDefault(t => t.Name == selected);
            if (tag != null)
            {
                _selectedTagId = tag.Id;
                _isFavoriteFilter = false; // 标签和收藏不同时启用
            }
        }

        UpdateChipVisualStates();
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

    /// <summary>
    /// 更新所有筛选Chip的视觉状态：标签文本 + 边框高亮。
    /// </summary>
    private void UpdateChipVisualStates()
    {
        // ---- 排序 Chip ----
        var sortLabels = new[]
        {
            AppText.SortNameAsc,
            AppText.SortNameDesc,
            AppText.SortTimeNew,
            AppText.SortTimeOld,
            AppText.SortSizeDesc,
            AppText.SortSizeAsc,
            AppText.SortByType
        };
        var sortText = _sortOption >= 0 && _sortOption < sortLabels.Length
            ? sortLabels[_sortOption]
            : sortLabels[0];
        SortChipLabel.Text = $"排序: {sortText}";
        SetChipActive(SortChip, _sortOption != 0);

        // ---- 筛选类型 Chip ----
        var filterText = _filterOption switch
        {
            1 => AppText.FilterFilesOnly,
            2 => AppText.FilterFoldersOnly,
            _ => AppText.FilterAll
        };
        FilterTypeChipLabel.Text = $"筛选: {filterText}";
        SetChipActive(FilterTypeChip, _filterOption != 0);

        // ---- 收藏 Chip ----
        if (_isFavoriteFilter)
        {
            FavChipIcon.Text = "★";
            FavChipLabel.Text = AppText.FavoritesFilterLabel;
            SetChipActive(FavChip, true);
        }
        else
        {
            FavChipIcon.Text = "☆";
            FavChipLabel.Text = AppText.FavoritesFilterLabel;
            SetChipActive(FavChip, false);
        }

        // ---- 标签 Chip ----
        if (_selectedTagId.HasValue)
        {
            var tagName = _tagsCache.FirstOrDefault(t => t.Id == _selectedTagId.Value)?.Name ?? "标签";
            TagsChipLabel.Text = tagName;
            SetChipActive(TagsChip, true);
        }
        else
        {
            TagsChipLabel.Text = AppText.FilterTags;
            SetChipActive(TagsChip, false);
        }
    }

    /// <summary>
    /// 设置单个Chip的高亮状态（边框颜色+字体权重）。
    /// 激活时显示蓝色边框，非激活时恢复Style默认值以支持暗色模式切换。
    /// </summary>
    private static void SetChipActive(Border chip, bool active)
    {
        if (active)
        {
            chip.Stroke = new SolidColorBrush(Color.FromArgb("#2563EB"));
            chip.StrokeThickness = 1.5;
        }
        else
        {
            chip.ClearValue(Border.StrokeProperty);
            chip.ClearValue(Border.StrokeThicknessProperty);
        }
    }

    private CloudDriveQueryOptions CreateQueryOptions()
    {
        var sortString = _sortOption switch
        {
            1 => "name desc",
            2 => "lastModificationTime desc",
            3 => "lastModificationTime asc",
            4 => "size desc",
            5 => "size asc",
            6 => "nodeType asc, name asc",
            _ => null
        };

        return new CloudDriveQueryOptions
        {
            SearchKeyword = string.IsNullOrWhiteSpace(FilesSearchBar.Text) ? null : FilesSearchBar.Text.Trim(),
            SearchScope = SearchAllSwitch.IsToggled ? "All" : "CurrentFolder",
            NodeType = _filterOption switch
            {
                1 => "File",
                2 => "Folder",
                _ => null
            },
            MediaType = null,
            Sorting = sortString,
            IsFavorite = _isFavoriteFilter ? true : null,
            TagId = _selectedTagId
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
