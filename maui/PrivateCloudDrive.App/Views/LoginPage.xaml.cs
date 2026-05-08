using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IWechatPlatformAuthService _wechatPlatformAuthService =
        AppServices.GetRequiredService<IWechatPlatformAuthService>();
    private WechatLoginSettings? _wechatSettings;
    private bool _isWechatAvailable;

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;
    public string ClientId => AppSettings.OAuthClientId;

    public LoginPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWechatSettingsAsync();
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async void OnWechatSignInClicked(object? sender, EventArgs e)
    {
        await SignInWithWechatAsync();
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async Task SignInAsync()
    {
        var userName = UserNameEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            ValidationLabel.Text = AppText.EnterUserNameAndPassword;
            ValidationLabel.IsVisible = true;
            return;
        }

        SetFormEnabled(false);
        ValidationLabel.IsVisible = false;

        try
        {
            await _authService.SignInAsync(userName, password);
            PasswordEntry.Text = string.Empty;
            await Shell.Current.GoToAsync("//files", true);
        }
        catch (Exception exception)
        {
            PasswordEntry.Text = string.Empty;
            ValidationLabel.Text = exception.Message;
            ValidationLabel.IsVisible = true;
        }
        finally
        {
            SetFormEnabled(true);
        }
    }

    private async Task SignInWithWechatAsync()
    {
        if (_wechatSettings?.IsEnabled != true || !_isWechatAvailable)
        {
            return;
        }

        SetFormEnabled(false);
        ValidationLabel.IsVisible = false;

        try
        {
            var authorization = await _wechatPlatformAuthService.AuthorizeAsync(_wechatSettings);
            if (!authorization.Succeeded || string.IsNullOrWhiteSpace(authorization.Code))
            {
                throw new InvalidOperationException(authorization.ErrorMessage ?? AppText.WechatSignInCanceled);
            }

            var signInResult = await _authService.SignInWithWechatCodeAsync(
                authorization.Code,
                authorization.State,
                authorization.Platform,
                deviceIdHash: null);

            if (signInResult.Succeeded)
            {
                await Shell.Current.GoToAsync("//files", true);
                return;
            }

            if (signInResult.BindingRequired)
            {
                await BindExistingAccountAndSignInAsync(signInResult.BindingTicket);
                return;
            }

            throw new InvalidOperationException(signInResult.ErrorMessage ?? AppText.WechatSignInFailed);
        }
        catch (Exception exception)
        {
            ValidationLabel.Text = exception.Message;
            ValidationLabel.IsVisible = true;
        }
        finally
        {
            SetFormEnabled(true);
        }
    }

    private async Task BindExistingAccountAndSignInAsync(string? bindingTicket)
    {
        var userName = UserNameEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bindingTicket) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            ValidationLabel.Text = AppText.EnterUserNamePasswordThenWechat;
            ValidationLabel.IsVisible = true;
            return;
        }

        await _apiClient.BindExistingWechatAsync(bindingTicket, userName, password);
        await _authService.SignInAsync(userName, password);
        PasswordEntry.Text = string.Empty;
        await Shell.Current.GoToAsync("//files", true);
    }

    private async Task LoadWechatSettingsAsync()
    {
        try
        {
            _wechatSettings = await _apiClient.GetWechatLoginSettingsAsync();
            _isWechatAvailable = _wechatSettings.IsEnabled &&
                                 await _wechatPlatformAuthService.IsAvailableAsync(_wechatSettings);
            WechatSignInButton.IsVisible = _isWechatAvailable;
            WechatSignInButton.IsEnabled = _isWechatAvailable;
        }
        catch
        {
            _wechatSettings = null;
            _isWechatAvailable = false;
            WechatSignInButton.IsVisible = false;
        }
    }

    private void SetFormEnabled(bool enabled)
    {
        UserNameEntry.IsEnabled = enabled;
        PasswordEntry.IsEnabled = enabled;
        SignInButton.IsEnabled = enabled;
        WechatSignInButton.IsEnabled = enabled && _wechatSettings?.IsEnabled == true && _isWechatAvailable;
        SignInButton.Text = enabled ? AppText.SignInAction : AppText.SigningIn;
        SignInLoadingPanel.IsVisible = !enabled;
        SignInLoadingIndicator.IsRunning = !enabled;
    }
}
