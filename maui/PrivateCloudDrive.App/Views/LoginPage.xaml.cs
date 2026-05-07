using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;
    public string ClientId => AppSettings.OAuthClientId;

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
            await _authService.SignInAsync();
            await Shell.Current.GoToAsync("//files", true);
        }
        catch (Exception exception)
        {
            ValidationLabel.Text = exception.Message;
            ValidationLabel.IsVisible = true;
        }
        finally
        {
            SignInButton.IsEnabled = true;
        }
    }
}
