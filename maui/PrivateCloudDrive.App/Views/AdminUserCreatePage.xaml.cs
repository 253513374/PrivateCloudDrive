using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 管理员创建用户页，支持指定用户名、邮箱、密码和初始角色。
/// </summary>
public partial class AdminUserCreatePage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    private const string RoleAdmin = "admin";
    private const string RoleUser = "user";

    /// <summary>
    /// 角色选择器值列表，与 Picker 索引对应。
    /// </summary>
    private static readonly string[] RoleValues = [RoleUser, RoleAdmin];

    public AdminUserCreatePage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        SetIdleState();
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        var userName = UserNameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(userName))
        {
            await DisplayAlertAsync(AppText.Format("提示", "请输入用户名"), string.Empty, "知道了");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlertAsync(AppText.Format("提示", "请输入邮箱"), string.Empty, "知道了");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync(AppText.Format("提示", "请输入密码"), string.Empty, "知道了");
            return;
        }

        if (password.Length < 6)
        {
            await DisplayAlertAsync(AppText.Format("提示", "密码长度不能少于 6 位"), string.Empty, "知道了");
            return;
        }

        SetLoadingState("正在创建用户");

        try
        {
            // 获取角色值：Picker.SelectedIndex → RoleValues[]
            var selectedIndex = RolePicker.SelectedIndex;
            var roleNames = selectedIndex >= 0 && selectedIndex < RoleValues.Length
                ? new[] { RoleValues[selectedIndex] }
                : null;

            var result = await _apiClient.CreateAdminUserAsync(
                userName,
                email,
                password,
                roleNames,
                CancellationToken.None);

            SetSuccessState($"用户 \"{result.UserName}\" 创建成功");
        }
        catch (Exception exception)
        {
            SetErrorState($"创建失败。{UserVisibleErrorSanitizer.ForSettings(exception)}");
        }
    }

    private void SetLoadingState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RetryButton.IsVisible = false;
        StateLabel.Text = message;
        CreateButton.IsEnabled = false;
        CreateResultLabel.IsVisible = false;
    }

    private void SetIdleState()
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = false;
        StatePanel.IsVisible = false;
        CreateButton.IsEnabled = true;
        CreateResultLabel.IsVisible = false;
    }

    private void SetErrorState(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;
        CreateButton.IsEnabled = true;
        CreateResultLabel.Text = message;
        CreateResultLabel.TextColor = Color.FromArgb("#D32F2F");
        CreateResultLabel.IsVisible = true;
    }

    private void SetSuccessState(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = false;
        StatePanel.IsVisible = false;
        CreateButton.IsEnabled = true;
        CreateResultLabel.Text = message;
        CreateResultLabel.TextColor = Color.FromArgb("#00C853");
        CreateResultLabel.IsVisible = true;

        // 清空输入框
        UserNameEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
        RolePicker.SelectedIndex = 0;
    }
}
