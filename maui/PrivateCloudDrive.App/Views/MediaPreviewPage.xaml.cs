using CommunityToolkit.Maui.Views;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

[QueryProperty(nameof(FileId), "id")]
[QueryProperty(nameof(FileName), "name")]
[QueryProperty(nameof(MediaKind), "kind")]
public partial class MediaPreviewPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private bool _loaded;

    public string FileId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string MediaKind { get; set; } = string.Empty;

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
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ErrorPanel.IsVisible = false;
        PreviewImage.IsVisible = false;
        VideoPlayer.IsVisible = false;
        VideoPlayer.Source = null;

        try
        {
            if (!Guid.TryParse(FileId, out var id))
            {
                throw new InvalidOperationException("Invalid media id.");
            }

            if (string.Equals(MediaKind, "Video", StringComparison.OrdinalIgnoreCase))
            {
                var source = await _apiClient.GetRemoteFileContentSourceAsync(id);
                VideoPlayer.Source = MediaSource.FromUri(source.Uri, source.Headers.ToDictionary());
                VideoPlayer.MetadataTitle = FileName;
                VideoPlayer.IsVisible = true;
                return;
            }

            var content = await _apiClient.GetFileContentAsync(id, thumbnail: false);
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(content.Content));
            PreviewImage.IsVisible = true;
        }
        catch (Exception exception)
        {
            ErrorLabel.Text = exception.Message;
            ErrorPanel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadPreviewAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        VideoPlayer.Stop();
        VideoPlayer.Source = null;
    }
}
