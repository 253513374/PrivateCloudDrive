using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class StartupPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
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

        _navigated = true;
        await Task.Delay(350);
        var isSignedIn = await _authService.IsSignedInAsync();
        await Shell.Current.GoToAsync(isSignedIn ? "//files" : "//login", true);
    }
}
