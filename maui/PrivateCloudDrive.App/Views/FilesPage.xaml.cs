using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;
#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace PrivateCloudDrive.App.Views;

public partial class FilesPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IUploadQueueService _uploadQueueService = AppServices.GetRequiredService<IUploadQueueService>();
    private readonly List<PathSegment> _path = [new(null, AppText.Files)];
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
        try
        {
            var files = await PickUploadFilesAsync();
            if (files.Count == 0)
            {
                return;
            }

            UploadStatusPanel.IsVisible = true;

            var failedUploads = new List<string>();

            foreach (var file in files)
            {
                UploadStatusLabel.Text = file.FileName;
                UploadProgressBar.Progress = 0;
                var queueItem = _uploadQueueService.Enqueue(file, CurrentPath);
                queueItem.MarkUploading();

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
            UploadStatusPanel.IsVisible = false;
            UploadProgressBar.Progress = 0;
        }
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
            var items = await _apiClient.GetItemsAsync(_currentFolderId);
            Items.Clear();

            foreach (var item in items)
            {
                Items.Add(item);
            }

            SetFilesIdleState();
        }
        catch (Exception exception)
        {
            Items.Clear();
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
    }

    private sealed record PathSegment(Guid? Id, string Name);
}
