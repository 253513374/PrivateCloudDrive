using Microsoft.Maui.ApplicationModel.DataTransfer;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

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
    private string _fileSize = string.Empty;
    private string _modifiedAt = string.Empty;
    private bool _isFavorite;

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
            _fileKind = value;
            OnPropertyChanged();
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
        ? "Favorited"
        : "Not favorited";

    public string FavoriteButtonText => _isFavorite
        ? "Remove favorite"
        : "Add favorite";

    public FileDetailsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Guid.TryParse(FileId, out _))
        {
            SetIdleState();
            return;
        }

        SetErrorState("File details are unavailable because the selected item id is invalid.");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnFavoriteClicked(object? sender, EventArgs e)
    {
        if (!TryGetFileId(out var fileId))
        {
            SetErrorState("File details are unavailable because the selected item id is invalid.");
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
            SetErrorState("File details are unavailable because the selected item id is invalid.");
            return;
        }

        var tagName = await DisplayPromptAsync(
            "Add tag",
            "Tag name",
            accept: "Add",
            cancel: "Cancel",
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
            ActionStateLabel.Text = $"Tag added: {tag.Name}";
        });
    }

    private async void OnCreateShareClicked(object? sender, EventArgs e)
    {
        if (!TryGetFileId(out var fileId))
        {
            SetErrorState("File details are unavailable because the selected item id is invalid.");
            return;
        }

        var daysText = await DisplayPromptAsync(
            "Share expiration",
            "Days",
            accept: "Next",
            cancel: "Cancel",
            initialValue: "7",
            maxLength: 4,
            keyboard: Keyboard.Numeric);

        if (daysText == null)
        {
            return;
        }

        if (!TryCreateExpiration(daysText, out var expirationTime))
        {
            await DisplayAlertAsync("Share not created", "Expiration days must be empty or greater than zero.", "OK");
            return;
        }

        var password = await DisplayPromptAsync(
            "Share password",
            "Optional password",
            accept: "Create",
            cancel: "Cancel",
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
                ? "Share link copied. Password required."
                : "Share link copied.";
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
