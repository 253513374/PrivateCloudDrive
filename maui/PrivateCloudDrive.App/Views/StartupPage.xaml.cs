using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示StartupPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class StartupPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private bool _checking;
    private bool _navigated;

    /// <summary>
    /// 初始化 <see cref="StartupPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public StartupPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_navigated)
        {
            return;
        }

        await CheckSignInAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await CheckSignInAsync();
    }

    private async Task CheckSignInAsync()
    {
        if (_checking || _navigated)
        {
            return;
        }

        _checking = true;
        SetLoadingState(AppText.CheckingSignInStatus);

        try
        {
            await Task.Delay(350);
            StartupStatusLabel.Text = AppText.RestoringSession;
            var isSignedIn = await _authService.IsSignedInAsync();
            _navigated = true;
            await Shell.Current.GoToAsync(isSignedIn ? "//files" : "//login", true);
        }
        catch (Exception exception)
        {
            SetErrorState(AppText.Format(nameof(AppText.UnableToRestoreSignInState), exception.Message));
        }
        finally
        {
            _checking = false;
        }
    }

    private void SetLoadingState(string message)
    {
        StartupErrorPanel.IsVisible = false;
        StartupStatusLabel.Text = message;
        StartupLoadingIndicator.IsVisible = true;
        StartupLoadingIndicator.IsRunning = true;
    }

    private void SetErrorState(string message)
    {
        StartupStatusLabel.Text = AppText.StartupFailed;
        StartupLoadingIndicator.IsRunning = false;
        StartupLoadingIndicator.IsVisible = false;
        StartupErrorLabel.Text = message;
        StartupErrorPanel.IsVisible = true;
    }
}
