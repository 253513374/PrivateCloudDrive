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

    /// <summary>
    /// 初始化 <see cref="MediaPreviewPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MediaPreviewPage()
    {
        InitializeComponent();
        BindingContext = this;
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
        StatusPanel.IsVisible = false;
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
            OnPropertyChanged(nameof(FileName));
            OnPropertyChanged(nameof(MediaKind));

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
                return;
            }

            var content = await _apiClient.GetFileContentAsync(id, thumbnail: false, cancellationToken);
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(content.Content));
            PreviewImage.IsVisible = true;
        }
        catch (OperationCanceledException)
        {
            // 用户离开页面或重新加载时取消旧请求，不需要显示错误。
        }
        catch (Exception exception)
        {
            ErrorLabel.Text = exception.Message;
            ErrorPanel.IsVisible = true;
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
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loadCancellationTokenSource?.Cancel();
        VideoPlayer.Stop();
        VideoPlayer.Source = null;
    }
}
