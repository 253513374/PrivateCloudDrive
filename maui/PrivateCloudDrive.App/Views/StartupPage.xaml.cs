using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class StartupPage : ContentPage
{
    private readonly MockCloudDriveApiClient _apiClient = new();
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
        await Shell.Current.GoToAsync(_apiClient.IsSignedIn ? "//files" : "//login", true);
    }
}
