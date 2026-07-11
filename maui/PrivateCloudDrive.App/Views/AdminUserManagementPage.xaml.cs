using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 管理员用户管理页，展示当前私有备份服务器的注册用户列表。
/// </summary>
public partial class AdminUserManagementPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public ObservableCollection<AdminUserItem> Users { get; } = [];

    public AdminUserManagementPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsersAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        SetLoadingState("正在读取用户列表");

        try
        {
            var users = await _apiClient.GetAdminUsersAsync();
            Users.Clear();

            foreach (var user in users)
            {
                Users.Add(AdminUserItem.FromDto(user));
            }

            PageSubtitle.Text = $"管理员 · 共 {Users.Count} 个用户";
            SetIdleState();
        }
        catch (Exception exception)
        {
            Users.Clear();
            SetErrorState($"无法读取用户列表。{UserVisibleErrorSanitizer.ForSettings(exception)}");
        }
    }

    private void SetLoadingState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RetryButton.IsVisible = false;
        StateLabel.Text = message;
    }

    private void SetIdleState()
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = false;
        StatePanel.IsVisible = false;
    }

    private void SetErrorState(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;
    }

    /// <summary>
    /// 管理员用户列表项，封装用户名、邮箱和角色显示。
    /// </summary>
    public sealed class AdminUserItem
    {
        public string UserName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string RolesText { get; init; } = string.Empty;

        public static AdminUserItem FromDto(AdminUserDto dto)
        {
            return new AdminUserItem
            {
                UserName = dto.UserName,
                Email = dto.Email,
                RolesText = dto.Roles is { Length: > 0 }
                    ? string.Join(", ", dto.Roles)
                    : "-"
            };
        }
    }
}
