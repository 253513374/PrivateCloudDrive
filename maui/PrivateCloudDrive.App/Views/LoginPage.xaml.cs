using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示LoginPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IWechatPlatformAuthService _wechatPlatformAuthService =
        AppServices.GetRequiredService<IWechatPlatformAuthService>();
    private WechatLoginSettings? _wechatSettings;
    private ExternalLoginSettings? _externalSettings;
    private bool _isWechatAvailable;
    private bool _isPasswordVisible;
    private CancellationTokenSource? _externalSignInCancellation;
    private const string GoogleProvider = "Google";
    private const string GitHubProvider = "GitHub";

    public string ClientId => AppSettings.OAuthClientId;

    /// <summary>
    /// 初始化 <see cref="LoginPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.WhenAll(
            LoadWechatSettingsAsync(),
            LoadExternalSettingsAsync());
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async void OnWechatSignInClicked(object? sender, EventArgs e)
    {
        await SignInWithWechatAsync();
    }

    private async void OnGoogleSignInClicked(object? sender, EventArgs e)
    {
        await SignInWithExternalAsync(GoogleProvider);
    }

    private async void OnGitHubSignInClicked(object? sender, EventArgs e)
    {
        await SignInWithExternalAsync(GitHubProvider);
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private void OnTogglePasswordVisibilityTapped(object? sender, TappedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        PasswordEntry.IsPassword = !_isPasswordVisible;
        PasswordHiddenIcon.IsVisible = !_isPasswordVisible;
        PasswordVisibleIcon.IsVisible = _isPasswordVisible;
    }

    private void OnCancelSignInClicked(object? sender, EventArgs e)
    {
        _externalSignInCancellation?.Cancel();
        ShowValidation(AppText.ExternalSignInCanceled);
        SetFormEnabled(true);
    }

    private async Task SignInAsync()
    {
        var userName = UserNameEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            ShowValidation(AppText.EnterUserNameAndPassword);
            return;
        }

        SetFormEnabled(false);
        HideValidation();

        try
        {
            await _authService.SignInAsync(userName, password);
            PasswordEntry.Text = string.Empty;
            await Shell.Current.GoToAsync("//files", true);
        }
        catch (Exception exception)
        {
            PasswordEntry.Text = string.Empty;
            ShowValidation(GetUserFacingSignInError(exception));
        }
        finally
        {
            SetFormEnabled(true);
        }
    }

    private static string GetUserFacingSignInError(Exception exception)
    {
        var message = exception.Message;
        if (message.Contains("Invalid username or password", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return AppText.InvalidUserNameOrPassword;
        }

        return message;
    }

    private async Task SignInWithWechatAsync()
    {
        if (_wechatSettings?.IsEnabled != true || !_isWechatAvailable)
        {
            return;
        }

        SetFormEnabled(false);
        HideValidation();

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
            ShowValidation(exception.Message);
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
            ShowValidation(AppText.EnterUserNamePasswordThenWechat);
            return;
        }

        await _apiClient.BindExistingWechatAsync(bindingTicket, userName, password);
        await _authService.SignInAsync(userName, password);
        PasswordEntry.Text = string.Empty;
        await Shell.Current.GoToAsync("//files", true);
    }

    private async Task SignInWithExternalAsync(string provider)
    {
        var providerSettings = _externalSettings?.GetProvider(provider);
        if (providerSettings?.IsEnabled != true)
        {
            return;
        }

        _externalSignInCancellation?.Cancel();
        _externalSignInCancellation?.Dispose();

        using var cancellation = new CancellationTokenSource();
        _externalSignInCancellation = cancellation;

        SetFormEnabled(false, canCancel: true);
        HideValidation();

        try
        {
            var authorization = await _authService.AuthorizeExternalAsync(providerSettings, cancellation.Token);
            var signInResult = await _authService.SignInWithExternalCodeAsync(
                provider,
                authorization.Code,
                authorization.State,
                authorization.RedirectUri,
                authorization.CodeVerifier,
                deviceIdHash: null,
                cancellation.Token);

            if (signInResult.Succeeded)
            {
                await Shell.Current.GoToAsync("//files", true);
                return;
            }

            if (signInResult.BindingRequired)
            {
                await BindExistingExternalAccountAndSignInAsync(signInResult.BindingTicket);
                return;
            }

            throw new InvalidOperationException(signInResult.ErrorMessage ?? AppText.ExternalSignInFailed);
        }
        catch (OperationCanceledException)
        {
            ShowValidation(AppText.ExternalSignInCanceled);
        }
        catch (TimeoutException)
        {
            ShowValidation(AppText.ExternalSignInTimedOut);
        }
        catch (Exception exception)
        {
            ShowValidation(exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_externalSignInCancellation, cancellation))
            {
                _externalSignInCancellation = null;
            }

            SetFormEnabled(true);
        }
    }

    private async Task BindExistingExternalAccountAndSignInAsync(string? bindingTicket)
    {
        var userName = UserNameEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bindingTicket) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowValidation(AppText.EnterUserNamePasswordThenExternal);
            return;
        }

        await _apiClient.BindExistingExternalAsync(bindingTicket, userName, password);
        await _authService.SignInAsync(userName, password);
        PasswordEntry.Text = string.Empty;
        await Shell.Current.GoToAsync("//files", true);
    }


    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.IsVisible = true;
        ValidationPanel.IsVisible = true;
    }

    private void HideValidation()
    {
        ValidationLabel.IsVisible = false;
        ValidationPanel.IsVisible = false;
    }

    private async Task LoadWechatSettingsAsync()
    {
        try
        {
            _wechatSettings = await _apiClient.GetWechatLoginSettingsAsync();
            if (!_wechatSettings.IsEnabled)
            {
                _isWechatAvailable = false;
                SetWechatEntryState(false, AppText.WechatSignInNotEnabled);
                return;
            }

            _isWechatAvailable = await _wechatPlatformAuthService.IsAvailableAsync(_wechatSettings);
            SetWechatEntryState(
                _isWechatAvailable,
                _isWechatAvailable ? null : AppText.WechatUnavailableOnThisDevice);
        }
        catch
        {
            _wechatSettings = null;
            _isWechatAvailable = false;
            SetWechatEntryState(false, AppText.UnableToLoadWechatSettings);
        }
    }

    private async Task LoadExternalSettingsAsync()
    {
        try
        {
            _externalSettings = await _apiClient.GetExternalLoginSettingsAsync();
            SetExternalEntryState(GoogleProvider);
            SetExternalEntryState(GitHubProvider);
        }
        catch
        {
            _externalSettings = null;
            SetExternalEntryState(GoogleProvider, false, AppText.UnableToLoadExternalSettings);
            SetExternalEntryState(GitHubProvider, false, AppText.UnableToLoadExternalSettings);
        }
    }

    private void SetFormEnabled(bool enabled, bool canCancel = false)
    {
        UserNameEntry.IsEnabled = enabled;
        PasswordEntry.IsEnabled = enabled;
        SignInButton.IsEnabled = enabled;
        SetExternalButtonAvailability(
            WechatSignInButton,
            enabled &&
            _wechatSettings?.IsEnabled == true &&
            _isWechatAvailable);
        SetExternalButtonAvailability(
            GoogleSignInButton,
            enabled && IsExternalProviderEnabled(GoogleProvider));
        SetExternalButtonAvailability(
            GitHubSignInButton,
            enabled && IsExternalProviderEnabled(GitHubProvider));
        SignInButton.Text = enabled ? AppText.SignInAction : AppText.SigningIn;
        SignInLoadingPanel.IsVisible = !enabled;
        SignInLoadingIndicator.IsRunning = !enabled;
        SignInCancelButton.IsVisible = !enabled && canCancel;
        SignInCancelButton.IsEnabled = !enabled && canCancel;
    }

    private void SetWechatEntryState(bool canSignIn, string? statusMessage)
    {
        SetExternalButtonAvailability(WechatSignInButton, canSignIn);
        WechatStatusLabel.Text = statusMessage ?? string.Empty;
        WechatStatusLabel.IsVisible = false;
    }

    private void SetExternalEntryState(string provider)
    {
        var providerSettings = _externalSettings?.GetProvider(provider);
        var canSignIn = providerSettings?.IsEnabled == true;
        SetExternalEntryState(provider, canSignIn, null);
    }

    private void SetExternalEntryState(string provider, bool canSignIn, string? statusMessage)
    {
        var button = GetExternalSignInButton(provider);
        var label = GetExternalStatusLabel(provider);

        SetExternalButtonAvailability(button, canSignIn);
        label.Text = statusMessage ?? string.Empty;
        label.IsVisible = false;
    }

    private bool IsExternalProviderEnabled(string provider)
    {
        return _externalSettings?.GetProvider(provider)?.IsEnabled == true;
    }

    private ImageButton GetExternalSignInButton(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? GitHubSignInButton
            : GoogleSignInButton;
    }

    private Label GetExternalStatusLabel(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? GitHubStatusLabel
            : GoogleStatusLabel;
    }

    private void SetExternalButtonAvailability(ImageButton button, bool canSignIn)
    {
        button.IsEnabled = true;
        button.InputTransparent = !canSignIn;
        button.Opacity = canSignIn ? 1 : 0.65;
    }

}
