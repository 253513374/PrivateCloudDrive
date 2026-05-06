using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly MockCloudDriveApiClient _apiClient = new();

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    public LoginPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        SignInButton.IsEnabled = false;
        ValidationLabel.IsVisible = false;

        try
        {
            var signedIn = await _apiClient.SignInAsync(UserNameEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
            if (!signedIn)
            {
                ValidationLabel.Text = "Enter a user name and password.";
                ValidationLabel.IsVisible = true;
                return;
            }

            await Shell.Current.GoToAsync("//files", true);
        }
        finally
        {
            SignInButton.IsEnabled = true;
        }
    }
}
