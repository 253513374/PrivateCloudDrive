using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class StartupPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private bool _checking;
    private bool _navigated;

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
        SetLoadingState("Checking sign-in status");

        try
        {
            await Task.Delay(350);
            StartupStatusLabel.Text = "Restoring session";
            var isSignedIn = await _authService.IsSignedInAsync();
            _navigated = true;
            await Shell.Current.GoToAsync(isSignedIn ? "//files" : "//login", true);
        }
        catch (Exception exception)
        {
            SetErrorState($"Unable to restore sign-in state. {exception.Message}");
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
        StartupStatusLabel.Text = "Startup failed";
        StartupLoadingIndicator.IsRunning = false;
        StartupLoadingIndicator.IsVisible = false;
        StartupErrorLabel.Text = message;
        StartupErrorPanel.IsVisible = true;
    }
}
