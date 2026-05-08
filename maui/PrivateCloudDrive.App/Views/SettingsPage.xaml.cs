using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IWechatPlatformAuthService _wechatPlatformAuthService =
        AppServices.GetRequiredService<IWechatPlatformAuthService>();
    private WechatLoginSettings? _wechatSettings;
    private bool _isWechatAvailable;

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSettingsStateAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadSettingsStateAsync();
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppText.SignOut,
            AppText.SignOutQuestion,
            AppText.SignOut,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        await _authService.SignOutAsync();
        await Shell.Current.GoToAsync("//login", true);
    }

    private async void OnOperationLogsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("operation-logs", true);
    }

    private async void OnTrashClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("trash", true);
    }

    private async void OnWechatBindClicked(object? sender, EventArgs e)
    {
        await BindWechatAsync();
    }

    private async void OnWechatUnbindClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppText.UnbindWechat,
            AppText.UnbindWechatQuestion,
            AppText.Unbind,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        SetWechatLoadingState();

        try
        {
            await _apiClient.UnbindWechatAsync();
            await LoadWechatStateAsync();
        }
        catch (Exception exception)
        {
            SetWechatInfoState(exception.Message, canBind: false, canUnbind: true);
        }
    }

    private async Task LoadSettingsStateAsync()
    {
        SetLoadingState(AppText.CheckingLocalSession);

        try
        {
            var isSignedIn = await _authService.IsSignedInAsync();
            SetInfoState(isSignedIn
                ? AppText.SignedInOnThisDevice
                : AppText.NoValidLocalSession);
            await LoadWechatStateAsync(isSignedIn);
        }
        catch (Exception exception)
        {
            SetErrorState(AppText.Format(nameof(AppText.UnableToReadLocalSession), exception.Message));
            SetWechatInfoState(AppText.Unavailable, canBind: false, canUnbind: false);
        }
    }

    private async Task LoadWechatStateAsync(bool? isSignedIn = null)
    {
        SetWechatLoadingState();

        try
        {
            _wechatSettings = await _apiClient.GetWechatLoginSettingsAsync();
            if (!_wechatSettings.IsEnabled)
            {
                _isWechatAvailable = false;
                SetWechatInfoState(AppText.NotEnabled, canBind: false, canUnbind: false);
                return;
            }

            _isWechatAvailable = await _wechatPlatformAuthService.IsAvailableAsync(_wechatSettings);

            var signedIn = isSignedIn ?? await _authService.IsSignedInAsync();
            if (!signedIn)
            {
                SetWechatInfoState(AppText.SignInRequired, canBind: false, canUnbind: false);
                return;
            }

            var binding = await _apiClient.GetWechatBindingAsync();
            if (binding == null)
            {
                SetWechatInfoState(
                    _isWechatAvailable ? AppText.NotBound : AppText.UnavailableOnThisDevice,
                    canBind: _isWechatAvailable,
                    canUnbind: false);
                return;
            }

            var name = string.IsNullOrWhiteSpace(binding.NickName)
                ? AppText.Bound
                : AppText.Format(nameof(AppText.BoundWithName), binding.NickName);
            SetWechatInfoState(name, canBind: false, canUnbind: true);
        }
        catch (Exception exception)
        {
            SetWechatInfoState(exception.Message, canBind: false, canUnbind: false);
        }
    }

    private async Task BindWechatAsync()
    {
        if (_wechatSettings?.IsEnabled != true || !_isWechatAvailable)
        {
            return;
        }

        SetWechatLoadingState();

        try
        {
            var authorization = await _wechatPlatformAuthService.AuthorizeAsync(_wechatSettings);
            if (!authorization.Succeeded || string.IsNullOrWhiteSpace(authorization.Code))
            {
                throw new InvalidOperationException(authorization.ErrorMessage ?? AppText.WechatAuthorizationCanceled);
            }

            await _apiClient.BindCurrentWechatAsync(
                authorization.Code,
                authorization.State,
                authorization.Platform,
                deviceIdHash: null);

            await LoadWechatStateAsync(isSignedIn: true);
        }
        catch (Exception exception)
        {
            SetWechatInfoState(exception.Message, canBind: true, canUnbind: false);
        }
    }

    private void SetLoadingState(string message)
    {
        SettingsStatePanel.IsVisible = true;
        SettingsLoadingIndicator.IsVisible = true;
        SettingsLoadingIndicator.IsRunning = true;
        SettingsRetryButton.IsVisible = false;
        SettingsStateLabel.Text = message;
    }

    private void SetInfoState(string message)
    {
        SettingsStatePanel.IsVisible = true;
        SettingsLoadingIndicator.IsRunning = false;
        SettingsLoadingIndicator.IsVisible = false;
        SettingsRetryButton.IsVisible = false;
        SettingsStateLabel.Text = message;
    }

    private void SetErrorState(string message)
    {
        SettingsStatePanel.IsVisible = true;
        SettingsLoadingIndicator.IsRunning = false;
        SettingsLoadingIndicator.IsVisible = false;
        SettingsRetryButton.IsVisible = true;
        SettingsStateLabel.Text = message;
    }

    private void SetWechatLoadingState()
    {
        WechatLoadingIndicator.IsVisible = true;
        WechatLoadingIndicator.IsRunning = true;
        WechatBindButton.IsVisible = false;
        WechatUnbindButton.IsVisible = false;
        WechatStatusLabel.Text = AppText.Checking;
    }

    private void SetWechatInfoState(string message, bool canBind, bool canUnbind)
    {
        WechatLoadingIndicator.IsRunning = false;
        WechatLoadingIndicator.IsVisible = false;
        WechatStatusLabel.Text = message;
        WechatBindButton.IsVisible = canBind;
        WechatBindButton.IsEnabled = canBind;
        WechatUnbindButton.IsVisible = canUnbind;
        WechatUnbindButton.IsEnabled = canUnbind;
    }
}
