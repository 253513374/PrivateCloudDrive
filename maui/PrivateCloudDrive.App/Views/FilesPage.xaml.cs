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
/// V1.4 UX-02: 新增多选模式、复选框、全选、批量操作进度反馈。
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
    private readonly HashSet<Guid> _selectedItemIds = [];
    private StorageUsage? _currentUsage;

    public ObservableCollection<SelectableCloudDriveItem> Items { get; } = [];

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    public string CurrentPath => string.Join(" / ", _path.Select(segment => segment.Name));

    public bool CanGoBack => _path.Count > 1;

    public bool ShowCurrentPath => CanGoBack;

    public string ItemCountText => Items.Count == 0
        ? "暂无项目"
        : $"{Items.Count} 个项目";

    /// <summary>
    /// 获取或查询当前是否处于搜索状态。
    /// </summary>
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(FilesSearchBar?.Text);

    /// <summary>
    /// 绑定属性：是否处于多选模式。XAML 中通过 DataTrigger 控制复选框等 UI 元素。
    /// </summary>
    public bool IsSelectionMode
    {
        get => _isSelectionMode;
        set
        {
            if (_isSelectionMode == value)
                return;
            _isSelectionMode = value;
            OnPropertyChanged();
        }
    }

    private int _currentSkipCount;
    private long _totalCount;
    private bool _isLoadingMore;
    private string? _previousSearchKeyword;
    private CancellationTokenSource? _searchDebounceCts;

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
        await LoadStorageUsageAsync();
        await LoadItemsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnSearchPressed(object? sender, EventArgs e)
    {
        CancelSearchDebounce();
        await LoadItemsAsync();
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        CancelSearchDebounce();
        await LoadItemsAsync();
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        CancelSearchDebounce();

        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        try
        {
            await Task.Delay(400, cts.Token);

            if (!cts.Token.IsCancellationRequested)
            {
                await LoadItemsAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled by new keystroke - expected
        }
        finally
        {
            if (cts == _searchDebounceCts)
            {
                _searchDebounceCts = null;
                cts.Dispose();
            }
        }
    }

    private void CancelSearchDebounce()
    {
        if (_searchDebounceCts is { IsCancellationRequested: false })
        {
            _searchDebounceCts.Cancel();
            _searchDebounceCts.Dispose();
            _searchDebounceCts = null;
        }
    }

    private async void OnFilterChanged(object? sender, EventArgs e)
    {
        if (!_filtersInitialized)
        {
            return;
        }

        _currentSkipCount = 0;
        _previousSearchKeyword = null;
        await LoadItemsAsync();
    }

    private async void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        _filtersInitialized = false;
        _previousSearchKeyword = null;
        _currentSkipCount = 0;
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

            if (!await CheckQuotaBeforeUploadAsync(files))
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



    /// <summary>
    /// 检查所选文件总大小是否超出剩余配额。
    /// </summary>
    private async Task<bool> CheckQuotaBeforeUploadAsync(IReadOnlyList<FileResult> files)
    {
        if (_currentUsage is null || !_currentUsage.IsQuotaConfigured)
        {
            return true;
        }

        var totalBytes = await GetTotalFileSizeAsync(files);
        if (totalBytes <= 0)
        {
            return true;
        }

        if (totalBytes > _currentUsage.RemainingBytes)
        {
            var totalText = FormatBytes(totalBytes);
            var remainingText = FormatBytes(_currentUsage.RemainingBytes);
            await DisplayAlertAsync(
                "容量不足",
                "" + totalText + "、但剩余容量仅 " + remainingText + "。\n请删除部分文件后再试，或联系管理员增加配额。",
                "知道了");
            return false;
        }

        return true;
    }

    private static async Task<long> GetTotalFileSizeAsync(IReadOnlyList<FileResult> files)
    {
        long total = 0;
        foreach (var file in files)
        {
            var size = await GetFileSizeAsync(file);
            if (size <= 0) { return 0; }
            total += size;
        }
        return total;
    }

    private static async Task<long> GetFileSizeAsync(FileResult file)
    {
        if (!string.IsNullOrEmpty(file.FullPath))
        {
            try
            {
                var fileInfo = new FileInfo(file.FullPath);
                if (fileInfo.Exists) { return fileInfo.Length; }
            }
            catch { }
        }
        try
        {
            using var stream = await file.OpenReadAsync();
            return stream.Length;
        }
        catch { return 0; }
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

            public IntPtr InitialDirectory;

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

    /// <summary>
    /// 处理文件项点击。多选模式下切换选中状态，正常模式下导航进入文件夹或文件详情。
    /// </summary>
    private async void OnFileItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not VisualElement { BindingContext: SelectableCloudDriveItem selectable })
        {
            return;
        }

        if (_isSelectionMode)
        {
            // Toggle selection
            selectable.IsSelected = !selectable.IsSelected;
            if (selectable.IsSelected)
                _selectedItemIds.Add(selectable.Id);
            else
                _selectedItemIds.Remove(selectable.Id);

            UpdateSelectedItems();
            return;
        }

        var item = selectable.Item;

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
        _currentSkipCount = 0;
        _previousSearchKeyword = null;
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

    private async void OnDetailsItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: SelectableCloudDriveItem selectable })
        {
            return;
        }

        var item = selectable.Item;
        var route = $"file-details?id={item.Id}&name={Uri.EscapeDataString(item.Name)}&kind={Uri.EscapeDataString(item.Kind)}&size={Uri.EscapeDataString(item.Size)}&modified={Uri.EscapeDataString(item.ModifiedAt)}&favorite={item.IsFavorite}";
        await Shell.Current.GoToAsync(route, true);
    }

    private async void OnDeleteItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: SelectableCloudDriveItem selectable })
        {
            return;
        }

        var item = selectable.Item;

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

    /// <summary>
    /// 进入或退出多选模式。
    /// </summary>
    private void SetSelectionMode(bool isSelectionMode)
    {
        if (!isSelectionMode)
        {
            // Exit selection mode: clear all selections
            _selectedItemIds.Clear();
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }

        IsSelectionMode = isSelectionMode;
        SelectionModeButton.Text = _isSelectionMode ? "完成" : "选择";
        BatchToolbar.IsVisible = _isSelectionMode;
        UpdateSelectedItems();
    }

    /// <summary>
    /// 获取当前选中的 SelectableCloudDriveItem 列表。
    /// </summary>
    private IReadOnlyList<SelectableCloudDriveItem> GetSelectedItems()
    {
        return Items.Where(item => item.IsSelected).ToList();
    }

    /// <summary>
    /// 更新选中计数和全选按钮文本。
    /// </summary>
    private void UpdateSelectedItems()
    {
        var count = _selectedItemIds.Count;
        SelectedCountLabel.Text = $"已选择 {count} 项";

        var allSelected = Items.Count > 0 && count == Items.Count;
        SelectAllButton.Text = allSelected ? "取消全选" : "全选";
    }

    /// <summary>
    /// 全选 / 取消全选。
    /// </summary>
    private void OnSelectAllClicked(object? sender, EventArgs e)
    {
        var allSelected = _selectedItemIds.Count == Items.Count;

        foreach (var item in Items)
        {
            item.IsSelected = !allSelected;
        }

        _selectedItemIds.Clear();
        if (!allSelected)
        {
            foreach (var item in Items)
            {
                _selectedItemIds.Add(item.Id);
            }
        }

        UpdateSelectedItems();
    }

    /// <summary>
    /// 显示批量操作进度指示器。
    /// </summary>
    private void ShowBatchProgress(string message)
    {
        BatchProgressPanel.IsVisible = true;
        BatchProgressLabel.Text = message;
    }

    /// <summary>
    /// 隐藏批量操作进度指示器。
    /// </summary>
    private void HideBatchProgress()
    {
        BatchProgressPanel.IsVisible = false;
        BatchProgressLabel.Text = string.Empty;
    }

    /// <summary>
    /// 批量删除（移入回收站）。
    /// </summary>
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
            ShowBatchProgress($"正在移入回收站 ({selectedItems.Count} 项)...");
            BatchToolbar.IsEnabled = false;

            await _apiClient.DeleteItemsAsync(selectedItems.Select(item => item.Id).ToList());

            HideBatchProgress();
            SetSelectionMode(false);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            HideBatchProgress();
            await DisplayAlertAsync(AppText.UnableToDelete, exception.Message, "OK");
        }
        finally
        {
            BatchToolbar.IsEnabled = true;
        }
    }

    /// <summary>
    /// 批量收藏。
    /// </summary>
    private async void OnBatchFavoriteClicked(object? sender, EventArgs e)
    {
        await SetSelectedFavoriteAsync(isFavorite: true);
    }

    /// <summary>
    /// 批量取消收藏。
    /// </summary>
    private async void OnBatchUnfavoriteClicked(object? sender, EventArgs e)
    {
        await SetSelectedFavoriteAsync(isFavorite: false);
    }

    /// <summary>
    /// 批量执行收藏/取消收藏。
    /// </summary>
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
            var actionText = isFavorite ? "收藏" : "取消收藏";
            ShowBatchProgress($"正在{actionText} ({selectedItems.Count} 项)...");
            BatchToolbar.IsEnabled = false;

            await _apiClient.SetFavoriteItemsAsync(selectedItems.Select(item => item.Id).ToList(), isFavorite);

            HideBatchProgress();
            SetSelectionMode(false);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            HideBatchProgress();
            await DisplayAlertAsync("无法更新收藏", exception.Message, "OK");
        }
        finally
        {
            BatchToolbar.IsEnabled = true;
        }
    }

    /// <summary>
    /// 批量移至根目录。
    /// </summary>
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
            ShowBatchProgress($"正在移动到根目录 ({selectedItems.Count} 项)...");
            BatchToolbar.IsEnabled = false;

            await _apiClient.MoveItemsAsync(selectedItems.Select(item => item.Id).ToList(), parentId: null);

            HideBatchProgress();
            SetSelectionMode(false);
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            HideBatchProgress();
            await DisplayAlertAsync("无法移动", exception.Message, "OK");
        }
        finally
        {
            BatchToolbar.IsEnabled = true;
        }
    }

    private async Task LoadItemsAsync()
    {
        if (_isLoadingMore)
        {
            await LoadMoreItemsAsync();
            return;
        }

        RefreshButton.IsEnabled = false;
        _currentSkipCount = 0;
        SetFilesLoadingState(IsSearchActive ? "搜索中..." : AppText.LoadingFiles);

        try
        {
            var keyword = FilesSearchBar.Text?.Trim();

            if (IsSearchActive && !string.IsNullOrWhiteSpace(keyword))
            {
                var options = CreateQueryOptions();
                var (items, totalCount) = await _apiClient.SearchItemsAsync(
                    keyword,
                    searchScope: options.SearchScope,
                    nodeType: options.NodeType,
                    mediaType: options.MediaType,
                    sorting: options.Sorting,
                    skipCount: 0,
                    maxResultCount: 50);

                _totalCount = totalCount;
                _currentSkipCount = items.Count;
                _previousSearchKeyword = keyword;

                ReplaceItems(items);
            }
            else
            {
                var items = await _apiClient.GetItemsAsync(_currentFolderId, options: CreateQueryOptions());
                _currentSkipCount = items.Count;
                _previousSearchKeyword = null;

                ReplaceItems(items);
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

    /// <summary>
    /// 用 API 返回的 CloudDriveItem 列表替换当前 Items，自动包装为 SelectableCloudDriveItem。
    /// 多选模式下恢复已有选中状态。
    /// </summary>
    private void ReplaceItems(IReadOnlyList<CloudDriveItem> newItems)
    {
        Items.Clear();

        foreach (var item in newItems)
        {
            var selectable = new SelectableCloudDriveItem(item);
            // Restore selection state if in multi-select mode
            if (_isSelectionMode && _selectedItemIds.Contains(item.Id))
            {
                selectable.IsSelected = true;
            }
            Items.Add(selectable);
        }

        UpdateSelectedItems();
    }

    private async Task LoadMoreItemsAsync()
    {
        try
        {
            var keyword = FilesSearchBar.Text?.Trim();

            if (IsSearchActive && !string.IsNullOrWhiteSpace(keyword))
            {
                var options = CreateQueryOptions();
                var (items, _) = await _apiClient.SearchItemsAsync(
                    keyword,
                    searchScope: options.SearchScope,
                    nodeType: options.NodeType,
                    mediaType: options.MediaType,
                    sorting: options.Sorting,
                    skipCount: _currentSkipCount,
                    maxResultCount: 50);

                foreach (var item in items)
                {
                    Items.Add(new SelectableCloudDriveItem(item));
                }

                _currentSkipCount += items.Count;
            }
            else
            {
                var items = await _apiClient.GetItemsAsync(
                    _currentFolderId,
                    skipCount: _currentSkipCount,
                    maxResultCount: 50,
                    options: CreateQueryOptions());

                foreach (var item in items)
                {
                    Items.Add(new SelectableCloudDriveItem(item));
                }

                _currentSkipCount += items.Count;
            }

            OnPropertyChanged(nameof(ItemCountText));
        }
        catch (AuthSessionExpiredException)
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            // Silently handle pagination error - user can scroll up and retry
            System.Diagnostics.Debug.WriteLine($"Pagination failed: {exception.Message}");
        }
        finally
        {
            _isLoadingMore = false;
        }
    }

    private async void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
    {
        if (_isLoadingMore)
        {
            return;
        }

        // Stop condition: no more pages to load
        if (IsSearchActive && _totalCount > 0 && _currentSkipCount >= _totalCount)
        {
            return;
        }

        _isLoadingMore = true;
        await LoadItemsAsync();
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
        UpdateEmptyView();
    }

    private void UpdateEmptyView()
    {
        if (IsSearchActive && Items.Count == 0)
        {
            FilesCollectionView.EmptyView = CreateSearchEmptyView();
        }
        else
        {
            // Restore default XAML-defined empty view
            FilesCollectionView.EmptyView = null;
        }
    }

    private static Border CreateSearchEmptyView()
    {
        return new Border
        {
            Padding = new Thickness(24),
            VerticalOptions = LayoutOptions.Start,
            HeightRequest = 230,
            BackgroundColor = Colors.Transparent,
            Stroke = Colors.Transparent,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Border
                    {
                        HeightRequest = 56,
                        WidthRequest = 56,
                        BackgroundColor = Colors.Transparent,
                        Stroke = Colors.Gray,
                        StrokeThickness = 1,
                        HorizontalOptions = LayoutOptions.Center,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                        Content = new Label
                        {
                            Text = "🔍",
                            FontSize = 24,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center
                        }
                    },
                    new Label
                    {
                        Text = AppText.NoSearchResults,
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = AppText.NoSearchResultsHelp,
                        FontSize = 13,
                        TextColor = Colors.Gray,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
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
        _currentUsage = usage;
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

    private sealed record PathSegment(Guid? Id, string Name);
}
