using CommunityToolkit.Maui.Views;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示MediaPreviewPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
[QueryProperty(nameof(FileId), "id")]
[QueryProperty(nameof(FileName), "name")]
[QueryProperty(nameof(MediaKind), "kind")]
public partial class MediaPreviewPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private CancellationTokenSource? _loadCancellationTokenSource;
    private MediaDetail? _detail;
    private Guid _currentFileId;
    private bool _loaded;

    public string FileId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string MediaKind { get; set; } = string.Empty;

    public string MediaKindText => _detail?.MediaType == MediaAssetMediaType.Video || MediaKind == "Video"
        ? "视频"
        : "图片";

    public string PreviewMetaText => _detail == null
        ? MediaKind
        : $"{MediaKindText} · {FormatSize(_detail.Size)}";

    public string PreviewDetailText
    {
        get
        {
            if (_detail == null)
            {
                return "正在读取媒体详情";
            }

            var shape = _detail.MediaType == MediaAssetMediaType.Video
                ? FormatDuration(_detail.DurationMilliseconds)
                : FormatDimensions(_detail.Width, _detail.Height);
            var timeText = _detail.TakenAt.HasValue
                ? $"拍摄于 {_detail.TakenAt.Value:yyyy/M/d HH:mm}"
                : "拍摄时间未知";
            return string.IsNullOrWhiteSpace(shape)
                ? $"{FormatSize(_detail.Size)} · {timeText}"
                : $"{shape} · {FormatSize(_detail.Size)} · {timeText}";
        }
    }

    public string PreviewProcessText => _detail == null
        ? "处理状态读取中"
        : _detail.ProcessStatus switch
        {
            MediaAssetProcessStatus.Pending => "等待后台处理，完成后会生成缩略图和预览。",
            MediaAssetProcessStatus.Processing => "后台处理中，稍后刷新可查看最新结果。",
            MediaAssetProcessStatus.Failed => string.IsNullOrWhiteSpace(_detail.ProcessErrorSummary)
                ? "处理失败，可以重新提交处理。"
                : $"处理失败：{_detail.ProcessErrorSummary}",
            MediaAssetProcessStatus.Completed => "处理完成，可正常预览。",
            _ => "处理状态未知。"
        };

    public bool CanRetryProcessing => _detail?.CanRetryProcessing == true;

    /// <summary>
    /// 初始化 <see cref="MediaPreviewPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MediaPreviewPage()
    {
        InitializeComponent();
        BindingContext = this;
        VideoPlayer.MediaFailed += OnVideoMediaFailed;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _loadCancellationTokenSource.Token;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ErrorPanel.IsVisible = false;
        ErrorRetryButton.IsVisible = true;
        StatusPanel.IsVisible = false;
        FormatNotSupportedPanel.IsVisible = false;
        PreviewImage.IsVisible = false;
        VideoPlayer.IsVisible = false;
        VideoPlayer.Source = null;

        try
        {
            if (!Guid.TryParse(FileId, out var id))
            {
                throw new InvalidOperationException(AppText.InvalidMediaId);
            }

            _currentFileId = id;
            _detail = await _apiClient.GetMediaDetailAsync(id, cancellationToken);
            FileName = string.IsNullOrWhiteSpace(FileName) ? _detail.Name : FileName;
            MediaKind = _detail.MediaType == MediaAssetMediaType.Video ? "Video" : "Image";
            NotifyPreviewDetailsChanged();

            if (!_detail.CanPreview)
            {
                ShowStatusPanel(_detail);
                return;
            }

            if (_detail.MediaType == MediaAssetMediaType.Video)
            {
                VideoPlayer.Source = await CreateVideoSourceAsync(id, cancellationToken);
                VideoPlayer.MetadataTitle = FileName;
                VideoPlayer.IsVisible = true;

                // If the video source is set but MediaElement can't play it,
                // the MediaFailed event will handle it.
                return;
            }

            var content = await _apiClient.GetFileContentAsync(id, thumbnail: false, cancellationToken);
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(content.Content));
            PreviewImage.IsVisible = true;
        }
        catch (HttpRequestException)
        {
            ShowNetworkError();
        }
        catch (TaskCanceledException)
        {
            ShowNetworkError();
        }
        catch (OperationCanceledException)
        {
            // 用户离开页面或重新加载时取消旧请求，不需要显示错误。
        }
        catch (Exception exception)
        {
            ShowLoadingError(exception.Message);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }
    }

    private void ShowStatusPanel(MediaDetail detail)
    {
        var statusText = detail.ProcessStatus switch
        {
            MediaAssetProcessStatus.Pending => "媒体正在等待后台处理，稍后刷新即可预览。",
            MediaAssetProcessStatus.Processing => "媒体正在处理中，完成后会显示封面和预览。",
            MediaAssetProcessStatus.Failed => string.IsNullOrWhiteSpace(detail.ProcessErrorSummary)
                ? "媒体处理失败，可以重新提交处理。"
                : $"媒体处理失败：{detail.ProcessErrorSummary}",
            _ => "媒体暂时不可预览。"
        };

        StatusLabel.Text = statusText;
        RetryProcessingButton.IsVisible = detail.CanRetryProcessing;
        StatusPanel.IsVisible = true;
    }

    private async Task<MediaSource> CreateVideoSourceAsync(Guid id, CancellationToken cancellationToken)
    {
#if WINDOWS
        var localPath = await _apiClient.DownloadFileToCacheAsync(id, FileName, cancellationToken);
        return MediaSource.FromFile(localPath);
#else
        var source = await _apiClient.GetRemoteFileContentSourceAsync(id, cancellationToken);
        return MediaSource.FromUri(source.Uri, source.Headers.ToDictionary());
#endif
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadPreviewAsync();
    }

    private async void OnRetryProcessingClicked(object? sender, EventArgs e)
    {
        if (_currentFileId == Guid.Empty)
        {
            return;
        }

        try
        {
            RetryProcessingButton.IsEnabled = false;
            RetryProcessingDetailButton.IsEnabled = false;
            await _apiClient.RetryMediaProcessingAsync(_currentFileId);
            await LoadPreviewAsync();
        }
        catch (Exception exception)
        {
            StatusLabel.Text = $"重新处理失败。{exception.Message}";
        }
        finally
        {
            RetryProcessingButton.IsEnabled = true;
            RetryProcessingDetailButton.IsEnabled = true;
        }
    }

    private void OnVideoMediaFailed(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowFormatNotSupported();
        });
    }

    private void ShowNetworkError()
    {
        ErrorLabel.Text = "网络连接异常，请检查网络后重试。";
        ErrorRetryButton.IsVisible = true;
        ErrorPanel.IsVisible = true;
    }

    private void ShowLoadingError(string details)
    {
        ErrorLabel.Text = string.IsNullOrWhiteSpace(details)
            ? "加载失败，请重试。"
            : $"加载失败：{details}";
        ErrorRetryButton.IsVisible = true;
        ErrorPanel.IsVisible = true;
    }

    private void ShowFormatNotSupported()
    {
        FormatNotSupportedLabel.Text = "当前设备暂不支持此格式";
        FormatNotSupportedDetailLabel.Text = "此视频格式在当前设备上无法播放。你可以尝试下载后使用其他播放器打开。";
        VideoPlayer.IsVisible = false;
        FormatNotSupportedPanel.IsVisible = true;
    }

    private void NotifyPreviewDetailsChanged()
    {
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(MediaKind));
        OnPropertyChanged(nameof(MediaKindText));
        OnPropertyChanged(nameof(PreviewMetaText));
        OnPropertyChanged(nameof(PreviewDetailText));
        OnPropertyChanged(nameof(PreviewProcessText));
        OnPropertyChanged(nameof(CanRetryProcessing));
    }

    private static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{size} {units[unitIndex]}" : $"{value:0.##} {units[unitIndex]}";
    }

    private static string FormatDuration(long? milliseconds)
    {
        if (!milliseconds.HasValue || milliseconds <= 0)
        {
            return string.Empty;
        }

        var duration = TimeSpan.FromMilliseconds(milliseconds.Value);
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes}:{duration.Seconds:00}";
    }

    private static string FormatDimensions(int? width, int? height)
    {
        return width.HasValue && height.HasValue && width > 0 && height > 0
            ? $"{width} x {height}"
            : string.Empty;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loadCancellationTokenSource?.Cancel();
        VideoPlayer.Stop();
        VideoPlayer.Source = null;
    }
}
