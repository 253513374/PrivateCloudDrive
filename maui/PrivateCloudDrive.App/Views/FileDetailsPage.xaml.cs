using Microsoft.Maui.ApplicationModel.DataTransfer;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示FileDetailsPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
[QueryProperty(nameof(FileId), "id")]
[QueryProperty(nameof(FileName), "name")]
[QueryProperty(nameof(FileKind), "kind")]
[QueryProperty(nameof(FileSize), "size")]
[QueryProperty(nameof(ModifiedAt), "modified")]
[QueryProperty(nameof(Favorite), "favorite")]
public partial class FileDetailsPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private string _fileId = string.Empty;
    private string _fileName = string.Empty;
    private string _fileKind = string.Empty;
    private string _rawFileKind = string.Empty;
    private string _fileSize = string.Empty;
    private string _modifiedAt = string.Empty;
    private bool _isFavorite;
    private bool _imagePreviewLoaded;

    public string FileId
    {
        get => _fileId;
        set
        {
            _fileId = value;
            OnPropertyChanged();
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }

    public string FileKind
    {
        get => _fileKind;
        set
        {
            _rawFileKind = value;
            _fileKind = AppText.FileKind(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsImageDetails));
        }
    }

    public string FileSize
    {
        get => _fileSize;
        set
        {
            _fileSize = value;
            OnPropertyChanged();
        }
    }

    public string ModifiedAt
    {
        get => _modifiedAt;
        set
        {
            _modifiedAt = value;
            OnPropertyChanged();
        }
    }

    public string Favorite
    {
        get => _isFavorite.ToString();
        set
        {
            _isFavorite = bool.TryParse(value, out var isFavorite) && isFavorite;
            NotifyFavoriteChanged();
        }
    }

    public string FavoriteStateText => _isFavorite
        ? AppText.Favorited
        : AppText.NotFavorited;

    public string FavoriteButtonText => _isFavorite
        ? AppText.RemoveFavorite
        : AppText.AddFavorite;

    public bool IsImageDetails => string.Equals(_rawFileKind, "Image", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 初始化 <see cref="FileDetailsPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileDetailsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Guid.TryParse(FileId, out _))
        {
            SetIdleState();
            if (IsImageDetails && !_imagePreviewLoaded)
            {
                await LoadImagePreviewAsync();
            }

            return;
        }

        SetErrorState(AppText.InvalidFileDetails);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnRetryImagePreviewClicked(object? sender, EventArgs e)
    {
        await LoadImagePreviewAsync();
    }

    private async void OnFavoriteClicked(object? sender, EventArgs e)
    {
        if (!TryGetFileId(out var fileId))
        {
            SetErrorState(AppText.InvalidFileDetails);
            return;
        }

        await RunActionAsync(async () =>
        {
            var updated = await _apiClient.SetFavoriteAsync(fileId, !_isFavorite);
            _isFavorite = updated.IsFavorite;
            NotifyFavoriteChanged();
            ActionStateLabel.Text = FavoriteStateText;
        });
    }

    private async void OnAddTagClicked(object? sender, EventArgs e)
    {
        if (!TryGetFileId(out var fileId))
        {
            SetErrorState(AppText.InvalidFileDetails);
            return;
        }

        var tagName = await DisplayPromptAsync(
            AppText.AddTag,
            AppText.TagName,
            accept: AppText.Add,
            cancel: AppText.Cancel,
            maxLength: 64,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return;
        }

        await RunActionAsync(async () =>
        {
            var normalizedName = tagName.Trim();
            var tags = await _apiClient.GetTagsAsync();
            var tag = tags.FirstOrDefault(item =>
                string.Equals(item.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

            tag ??= await _apiClient.CreateTagAsync(normalizedName, "#2F6FED");
            await _apiClient.AddTagToItemAsync(fileId, tag.Id);
            ActionStateLabel.Text = AppText.Format(nameof(AppText.TagAdded), tag.Name);
        });
    }

    private async void OnCreateShareClicked(object? sender, EventArgs e)
    {
        if (!TryGetFileId(out var fileId))
        {
            SetErrorState(AppText.InvalidFileDetails);
            return;
        }

        var daysText = await DisplayPromptAsync(
            AppText.ShareExpiration,
            AppText.Days,
            accept: AppText.Next,
            cancel: AppText.Cancel,
            initialValue: "7",
            maxLength: 4,
            keyboard: Keyboard.Numeric);

        if (daysText == null)
        {
            return;
        }

        if (!TryCreateExpiration(daysText, out var expirationTime))
        {
            await DisplayAlertAsync(AppText.ShareNotCreated, AppText.ExpirationDaysInvalid, "OK");
            return;
        }

        var password = await DisplayPromptAsync(
            AppText.SharePassword,
            AppText.OptionalPassword,
            accept: AppText.Create,
            cancel: AppText.Cancel,
            maxLength: 128,
            keyboard: Keyboard.Text);

        if (password == null)
        {
            return;
        }

        await RunActionAsync(async () =>
        {
            var share = await _apiClient.CreateShareAsync(
                fileId,
                expirationTime,
                allowDownload: true,
                string.IsNullOrWhiteSpace(password) ? null : password);

            var link = $"{AppSettings.ApiBaseUrl.TrimEnd('/')}/api/public/shares/{share.Token}";
            await Clipboard.Default.SetTextAsync(link);
            ActionStateLabel.Text = share.RequiresPassword
                ? AppText.Get(nameof(AppText.ShareLinkCopiedPasswordRequired))
                : AppText.Get(nameof(AppText.ShareLinkCopied));
        });
    }

    private void SetIdleState()
    {
        DetailsStatePanel.IsVisible = false;
        DetailsLoadingIndicator.IsRunning = false;
        DetailsLoadingIndicator.IsVisible = false;
        DetailsBackButton.IsVisible = false;
    }

    private void SetErrorState(string message)
    {
        DetailsStatePanel.IsVisible = true;
        DetailsLoadingIndicator.IsRunning = false;
        DetailsLoadingIndicator.IsVisible = false;
        DetailsBackButton.IsVisible = true;
        DetailsStateLabel.Text = message;
    }

    private async Task LoadImagePreviewAsync()
    {
        if (!TryGetFileId(out var fileId))
        {
            SetImagePreviewError(AppText.InvalidFileDetails);
            return;
        }

        _imagePreviewLoaded = true;
        SetImagePreviewLoading();

        try
        {
            FileContentResult content;
            try
            {
                content = await _apiClient.GetFileContentAsync(fileId, thumbnail: true);
            }
            catch
            {
                content = await _apiClient.GetFileContentAsync(fileId, thumbnail: false);
            }

            DetailsPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(content.Content));
            DetailsPreviewImage.IsVisible = true;
            ImagePreviewStatusPanel.IsVisible = false;
            ImagePreviewLoadingIndicator.IsRunning = false;
        }
        catch (Exception exception)
        {
            SetImagePreviewError(AppText.Format(nameof(AppText.UnableToLoadImagePreview), exception.Message));
        }
    }

    private void SetImagePreviewLoading()
    {
        DetailsPreviewImage.IsVisible = false;
        ImagePreviewStatusPanel.IsVisible = true;
        ImagePreviewLoadingIndicator.IsVisible = true;
        ImagePreviewLoadingIndicator.IsRunning = true;
        ImagePreviewRetryButton.IsVisible = false;
        ImagePreviewStatusLabel.Text = AppText.LoadingImagePreview;
        ImagePreviewStatusLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? (Color)Application.Current.Resources["TextSecondaryDark"]
            : (Color)Application.Current!.Resources["TextSecondaryLight"];
    }

    private void SetImagePreviewError(string message)
    {
        DetailsPreviewImage.IsVisible = false;
        ImagePreviewStatusPanel.IsVisible = true;
        ImagePreviewLoadingIndicator.IsRunning = false;
        ImagePreviewLoadingIndicator.IsVisible = false;
        ImagePreviewRetryButton.IsVisible = true;
        ImagePreviewStatusLabel.Text = message;
        ImagePreviewStatusLabel.TextColor = (Color)Application.Current!.Resources["Danger"];
    }

    private async Task RunActionAsync(Func<Task> action)
    {
        SetActionBusy(true);

        try
        {
            await action();
        }
        catch (Exception exception)
        {
            ActionStateLabel.Text = exception.Message;
        }
        finally
        {
            SetActionBusy(false);
        }
    }

    private void SetActionBusy(bool isBusy)
    {
        FavoriteButton.IsEnabled = !isBusy;
    }

    private bool TryGetFileId(out Guid fileId)
    {
        return Guid.TryParse(FileId, out fileId);
    }

    private static bool TryCreateExpiration(string daysText, out DateTime? expirationTime)
    {
        expirationTime = null;

        if (string.IsNullOrWhiteSpace(daysText))
        {
            return true;
        }

        if (!int.TryParse(daysText.Trim(), out var days) || days <= 0)
        {
            return false;
        }

        expirationTime = DateTime.Now.AddDays(days);
        return true;
    }

    private void NotifyFavoriteChanged()
    {
        OnPropertyChanged(nameof(Favorite));
        OnPropertyChanged(nameof(FavoriteStateText));
        OnPropertyChanged(nameof(FavoriteButtonText));
    }
}
