using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示SettingsPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private readonly IWechatPlatformAuthService _wechatPlatformAuthService =
        AppServices.GetRequiredService<IWechatPlatformAuthService>();
    private WechatLoginSettings? _wechatSettings;
    private ExternalLoginSettings? _externalSettings;
    private bool _isWechatAvailable;
    private const string GoogleProvider = "Google";
    private const string GitHubProvider = "GitHub";

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    /// <summary>
    /// 初始化 <see cref="SettingsPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = this;
        LoadApiBaseUrlState();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSettingsStateAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadSettingsStateAsync();
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppText.SignOut,
            AppText.SignOutQuestion,
            AppText.SignOut,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        await _authService.SignOutAsync();
        await Shell.Current.GoToAsync("//login", true);
    }

    private async void OnOperationLogsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("operation-logs", true);
    }

    private async void OnTrashClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("trash", true);
    }

    private async void OnSharesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("shares", true);
    }

    private async void OnAdminUserManagementClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("admin-users", true);
    }

    private async void OnSystemHealthClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("storage-usage", true);
    }

    private async void OnStorageConfigClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("storage-usage", true);
    }

    private async void OnMediaTasksClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("media-processing", true);
    }

    private async void OnShareRiskClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("share-risk", true);
    }

    private async void OnStorageUsageClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("storage-usage", true);
    }

    private async void OnSaveApiBaseUrlClicked(object? sender, EventArgs e)
    {
        try
        {
            AppSettings.SetApiBaseUrl(ApiBaseUrlEntry.Text ?? string.Empty);
            await _authService.SignOutAsync();
            LoadApiBaseUrlState();
            await DisplayAlertAsync("后端地址已保存", "请使用当前私有备份服务器的账号重新登录。", "知道了");
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("后端地址无效", UserVisibleErrorSanitizer.ForSettings(exception), "知道了");
        }
    }

    private async void OnResetApiBaseUrlClicked(object? sender, EventArgs e)
    {
        AppSettings.ResetApiBaseUrl();
        await _authService.SignOutAsync();
        LoadApiBaseUrlState();
        await DisplayAlertAsync("已恢复默认后端", "请使用默认私有备份服务器重新登录。", "知道了");
        await Shell.Current.GoToAsync("//login", true);
    }

    private async void OnWechatBindClicked(object? sender, EventArgs e)
    {
        await BindWechatAsync();
    }

    private async void OnWechatUnbindClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppText.UnbindWechat,
            AppText.UnbindWechatQuestion,
            AppText.Unbind,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        SetWechatLoadingState();

        try
        {
            await _apiClient.UnbindWechatAsync();
            await LoadWechatStateAsync();
        }
        catch (Exception exception)
        {
            SetWechatInfoState(UserVisibleErrorSanitizer.ForSettings(exception), canBind: false, canUnbind: true);
        }
    }

    private async void OnGoogleBindClicked(object? sender, EventArgs e)
    {
        await BindExternalAsync(GoogleProvider);
    }

    private async void OnGitHubBindClicked(object? sender, EventArgs e)
    {
        await BindExternalAsync(GitHubProvider);
    }

    private async void OnGoogleUnbindClicked(object? sender, EventArgs e)
    {
        await UnbindExternalAsync(GoogleProvider);
    }

    private async void OnGitHubUnbindClicked(object? sender, EventArgs e)
    {
        await UnbindExternalAsync(GitHubProvider);
    }

    private async Task LoadSettingsStateAsync()
    {
        SetLoadingState(AppText.CheckingLocalSession);
        LoadApiBaseUrlState();

        try
        {
            var isSignedIn = await _authService.IsSignedInAsync();
            SetInfoState(isSignedIn
                ? AppText.SignedInOnThisDevice
                : AppText.NoValidLocalSession);
            await LoadStorageUsageAsync(isSignedIn);
            await LoadSystemHealthAsync(isSignedIn);
            await LoadWechatStateAsync(isSignedIn);
            await LoadExternalStateAsync(isSignedIn);
            await CheckAdminAccessAsync(isSignedIn);
        }
        catch (Exception exception)
        {
            SetErrorState(UserVisibleErrorSanitizer.ForSettings(exception, AppText.Format(nameof(AppText.UnableToReadLocalSession), "请重新登录后重试")));
            SetSystemHealthUnavailable(UserVisibleErrorSanitizer.ForSystemHealth(exception));
            SetWechatInfoState(AppText.Unavailable, canBind: false, canUnbind: false);
            SetExternalInfoState(GoogleProvider, AppText.Unavailable, canBind: false, canUnbind: false);
            SetExternalInfoState(GitHubProvider, AppText.Unavailable, canBind: false, canUnbind: false);
            AdminSectionPanel.IsVisible = false;
        }
    }

    private async Task CheckAdminAccessAsync(bool isSignedIn)
    {
        AdminSectionPanel.IsVisible = false;

        if (!isSignedIn)
        {
            return;
        }

        try
        {
            await _apiClient.GetAdminUsersAsync();
            AdminSectionPanel.IsVisible = true;
        }
        catch
        {
            AdminSectionPanel.IsVisible = false;
        }
    }

    private async Task LoadStorageUsageAsync(bool isSignedIn)
    {
        if (!isSignedIn)
        {
            AccountFilesStatLabel.Text = "--";
            AccountMemoriesStatLabel.Text = "未登录";
            AccountCapacityStatLabel.Text = "--";
            StorageUsageLabel.Text = AppText.SignInRequired;
            StorageQuotaLabel.Text = string.Empty;
            StorageProgressBar.Progress = 0;
            return;
        }

        try
        {
            var usage = await _apiClient.GetStorageUsageAsync();
            AccountFilesStatLabel.Text = "在线";
            AccountMemoriesStatLabel.Text = "真实";
            StorageUsageLabel.Text = $"{FormatBytes(usage.UsedBytes)} 已使用";

            if (usage.IsQuotaConfigured)
            {
                AccountCapacityStatLabel.Text = $"{usage.UsagePercent:0.#}%";
                StorageQuotaLabel.Text = $"配额 {FormatBytes(usage.QuotaBytes)}，剩余 {FormatBytes(usage.RemainingBytes)}";
                StorageProgressBar.Progress = Math.Clamp((double)usage.UsagePercent / 100, 0, 1);
            }
            else
            {
                AccountCapacityStatLabel.Text = "无限";
                StorageQuotaLabel.Text = "未配置容量上限";
                StorageProgressBar.Progress = 0;
            }
        }
        catch (AuthSessionExpiredException)
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            AccountFilesStatLabel.Text = "异常";
            AccountMemoriesStatLabel.Text = "待重试";
            AccountCapacityStatLabel.Text = "--";
            StorageUsageLabel.Text = UserVisibleErrorSanitizer.ForStorage(exception);
            StorageQuotaLabel.Text = string.Empty;
            StorageProgressBar.Progress = 0;
        }
    }

    private async Task LoadSystemHealthAsync(bool isSignedIn)
    {
        if (!isSignedIn)
        {
            SystemHealthStatusLabel.Text = AppText.SignInRequired;
            SystemHealthDetailLabel.Text = "登录后可查看 API、存储和容量健康状态";
            SystemHealthDiagnosticsLabel.Text = string.Empty;
            StorageLocationLabel.Text = "存储位置：登录后读取";
            BackupScopeLabel.Text = "恢复边界：登录后读取";
            PrivacyBoundaryLabel.Text = "隐私边界：登录后读取";
            return;
        }

        try
        {
            var health = await _apiClient.GetSystemHealthSummaryAsync();
            SystemHealthStatusLabel.Text = health.OverallStatus switch
            {
                SystemHealthStatus.Healthy => "运行正常",
                SystemHealthStatus.Degraded => "部分降级",
                SystemHealthStatus.Unhealthy => "需要处理",
                _ => AppText.Unknown
            };
            SystemHealthDetailLabel.Text =
                $"API {FormatHealthStatus(health.ApiStatus)} · DB {FormatHealthStatus(health.DatabaseStatus)} · Redis {FormatHealthStatus(health.RedisStatus)} · " +
                $"存储 {UserVisibleErrorSanitizer.RedactUserVisibleText(health.StorageProvider, "当前存储后端")} {FormatHealthStatus(health.StorageStatus)} · " +
                $"FFmpeg {FormatHealthStatus(health.FfmpegStatus)} · FFprobe {FormatHealthStatus(health.FfprobeStatus)}";
            SystemHealthDiagnosticsLabel.Text = health.Diagnostics.Count == 0
                ? $"更新时间 {health.GeneratedAt:yyyy-MM-dd HH:mm}"
                : $"{FormatStorageDiskSpace(health)}；{string.Join("；", health.Diagnostics.Take(6).Select(item => UserVisibleErrorSanitizer.RedactUserVisibleText(item, "诊断详情已隐藏")))}";
            StorageLocationLabel.Text = string.IsNullOrWhiteSpace(health.StorageLocationDescription)
                ? "存储位置：服务器未返回可展示说明"
                : $"存储位置：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.StorageLocationDescription, "服务器已返回存储位置说明，详细位置已隐藏")}";
            BackupScopeLabel.Text = string.IsNullOrWhiteSpace(health.BackupScopeDescription)
                ? "恢复边界：请备份数据库、文件存储和部署配置。"
                : $"恢复边界：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.BackupScopeDescription, "服务器已返回恢复边界说明，敏感细节已隐藏")}";
            PrivacyBoundaryLabel.Text = string.IsNullOrWhiteSpace(health.PrivacyBoundaryDescription)
                ? "隐私边界：文件保存到当前连接的私有后端。"
                : $"隐私边界：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.PrivacyBoundaryDescription, "服务器已返回隐私边界说明，敏感细节已隐藏")}";
        }
        catch (AuthSessionExpiredException)
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            SetSystemHealthUnavailable(UserVisibleErrorSanitizer.ForSystemHealth(exception));
        }
    }

    private async Task LoadWechatStateAsync(bool? isSignedIn = null)
    {
        SetWechatLoadingState();

        try
        {
            _wechatSettings = await _apiClient.GetWechatLoginSettingsAsync();
            if (!_wechatSettings.IsEnabled)
            {
                _isWechatAvailable = false;
                SetWechatInfoState(AppText.NotEnabled, canBind: false, canUnbind: false);
                return;
            }

            _isWechatAvailable = await _wechatPlatformAuthService.IsAvailableAsync(_wechatSettings);

            var signedIn = isSignedIn ?? await _authService.IsSignedInAsync();
            if (!signedIn)
            {
                SetWechatInfoState(AppText.SignInRequired, canBind: false, canUnbind: false);
                return;
            }

            var binding = await _apiClient.GetWechatBindingAsync();
            if (binding == null)
            {
                SetWechatInfoState(
                    _isWechatAvailable ? AppText.NotBound : AppText.UnavailableOnThisDevice,
                    canBind: _isWechatAvailable,
                    canUnbind: false);
                return;
            }

            var name = string.IsNullOrWhiteSpace(binding.NickName)
                ? AppText.Bound
                : AppText.Format(nameof(AppText.BoundWithName), binding.NickName);
            SetWechatInfoState(name, canBind: false, canUnbind: true);
        }
        catch (Exception exception)
        {
            SetWechatInfoState(UserVisibleErrorSanitizer.ForSettings(exception), canBind: false, canUnbind: false);
        }
    }

    private async Task BindWechatAsync()
    {
        if (_wechatSettings?.IsEnabled != true || !_isWechatAvailable)
        {
            return;
        }

        SetWechatLoadingState();

        try
        {
            var authorization = await _wechatPlatformAuthService.AuthorizeAsync(_wechatSettings);
            if (!authorization.Succeeded || string.IsNullOrWhiteSpace(authorization.Code))
            {
                throw new InvalidOperationException(authorization.ErrorMessage ?? AppText.WechatAuthorizationCanceled);
            }

            await _apiClient.BindCurrentWechatAsync(
                authorization.Code,
                authorization.State,
                authorization.Platform,
                deviceIdHash: null);

            await LoadWechatStateAsync(isSignedIn: true);
        }
        catch (Exception exception)
        {
            SetWechatInfoState(UserVisibleErrorSanitizer.ForSettings(exception), canBind: true, canUnbind: false);
        }
    }

    private async Task LoadExternalStateAsync(bool? isSignedIn = null)
    {
        SetExternalLoadingState(GoogleProvider);
        SetExternalLoadingState(GitHubProvider);

        try
        {
            _externalSettings = await _apiClient.GetExternalLoginSettingsAsync();
            var signedIn = isSignedIn ?? await _authService.IsSignedInAsync();
            var bindings = signedIn
                ? await _apiClient.GetExternalBindingsAsync()
                : [];

            SetExternalProviderState(GoogleProvider, signedIn, bindings);
            SetExternalProviderState(GitHubProvider, signedIn, bindings);
        }
        catch (Exception exception)
        {
            var safeMessage = UserVisibleErrorSanitizer.ForSettings(exception);
            SetExternalInfoState(GoogleProvider, safeMessage, canBind: false, canUnbind: false);
            SetExternalInfoState(GitHubProvider, safeMessage, canBind: false, canUnbind: false);
        }
    }

    private void SetExternalProviderState(
        string provider,
        bool signedIn,
        IReadOnlyList<ExternalBinding> bindings)
    {
        var providerSettings = _externalSettings?.GetProvider(provider);
        if (providerSettings?.IsEnabled != true)
        {
            SetExternalInfoState(provider, AppText.NotEnabled, canBind: false, canUnbind: false);
            return;
        }

        if (!signedIn)
        {
            SetExternalInfoState(provider, AppText.SignInRequired, canBind: false, canUnbind: false);
            return;
        }

        var binding = bindings.FirstOrDefault(item =>
            string.Equals(item.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (binding == null)
        {
            SetExternalInfoState(provider, AppText.NotBound, canBind: true, canUnbind: false);
            return;
        }

        var name = string.IsNullOrWhiteSpace(binding.DisplayName)
            ? AppText.Bound
            : AppText.Format(nameof(AppText.BoundWithName), binding.DisplayName);
        SetExternalInfoState(provider, name, canBind: false, canUnbind: true);
    }

    private async Task BindExternalAsync(string provider)
    {
        var providerSettings = _externalSettings?.GetProvider(provider);
        if (providerSettings?.IsEnabled != true)
        {
            return;
        }

        SetExternalLoadingState(provider);

        try
        {
            var authorization = await _authService.AuthorizeExternalAsync(providerSettings);
            await _apiClient.BindCurrentExternalAsync(
                provider,
                authorization.Code,
                authorization.State,
                authorization.RedirectUri,
                authorization.CodeVerifier,
                deviceIdHash: null);

            await LoadExternalStateAsync(isSignedIn: true);
        }
        catch (OperationCanceledException)
        {
            SetExternalInfoState(provider, AppText.ExternalAuthorizationCanceled, canBind: true, canUnbind: false);
        }
        catch (TimeoutException)
        {
            SetExternalInfoState(provider, AppText.ExternalSignInTimedOut, canBind: true, canUnbind: false);
        }
        catch (Exception exception)
        {
            SetExternalInfoState(provider, UserVisibleErrorSanitizer.ForSettings(exception), canBind: true, canUnbind: false);
        }
    }

    private async Task UnbindExternalAsync(string provider)
    {
        var confirmed = await DisplayAlertAsync(
            GetExternalUnbindTitle(provider),
            GetExternalUnbindQuestion(provider),
            AppText.Unbind,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        SetExternalLoadingState(provider);

        try
        {
            await _apiClient.UnbindExternalAsync(provider);
            await LoadExternalStateAsync(isSignedIn: true);
        }
        catch (Exception exception)
        {
            SetExternalInfoState(provider, UserVisibleErrorSanitizer.ForSettings(exception), canBind: false, canUnbind: true);
        }
    }

    private void SetLoadingState(string message)
    {
        SettingsStatePanel.IsVisible = true;
        SettingsLoadingIndicator.IsVisible = true;
        SettingsLoadingIndicator.IsRunning = true;
        SettingsRetryButton.IsVisible = false;
        SettingsStateLabel.Text = message;
    }

    private void LoadApiBaseUrlState()
    {
        ApiBaseUrlEntry.Text = string.Empty;
        ApiBaseUrlStatusLabel.Text = UserVisibleErrorSanitizer.SafeServerLabel(AppSettings.HasCustomApiBaseUrl);
    }

    private void SetInfoState(string message)
    {
        SettingsStatePanel.IsVisible = true;
        SettingsLoadingIndicator.IsRunning = false;
        SettingsLoadingIndicator.IsVisible = false;
        SettingsRetryButton.IsVisible = false;
        SettingsStateLabel.Text = message;
    }

    private void SetErrorState(string message)
    {
        SettingsStatePanel.IsVisible = true;
        SettingsLoadingIndicator.IsRunning = false;
        SettingsLoadingIndicator.IsVisible = false;
        SettingsRetryButton.IsVisible = true;
        SettingsStateLabel.Text = message;
    }

    private void SetSystemHealthUnavailable(string message)
    {
        SystemHealthStatusLabel.Text = "无法读取系统健康状态";
        SystemHealthDetailLabel.Text = message;
        SystemHealthDiagnosticsLabel.Text = string.Empty;
        StorageLocationLabel.Text = "存储位置：无法读取";
        BackupScopeLabel.Text = "恢复边界：无法读取";
        PrivacyBoundaryLabel.Text = "隐私边界：无法读取";
    }

    private static string FormatHealthStatus(SystemHealthStatus status)
    {
        return status switch
        {
            SystemHealthStatus.Healthy => "正常",
            SystemHealthStatus.Degraded => "降级",
            SystemHealthStatus.Unhealthy => "异常",
            _ => AppText.Unknown
        };
    }

    private void SetWechatLoadingState()
    {
        WechatLoadingIndicator.IsVisible = true;
        WechatLoadingIndicator.IsRunning = true;
        WechatBindButton.IsVisible = false;
        WechatUnbindButton.IsVisible = false;
        WechatStatusLabel.Text = AppText.Checking;
    }

    private void SetWechatInfoState(string message, bool canBind, bool canUnbind)
    {
        WechatLoadingIndicator.IsRunning = false;
        WechatLoadingIndicator.IsVisible = false;
        WechatStatusLabel.Text = message;
        WechatBindButton.IsVisible = canBind;
        WechatBindButton.IsEnabled = canBind;
        WechatUnbindButton.IsVisible = canUnbind;
        WechatUnbindButton.IsEnabled = canUnbind;
    }

    private void SetExternalLoadingState(string provider)
    {
        var loadingIndicator = GetExternalLoadingIndicator(provider);
        var bindButton = GetExternalBindButton(provider);
        var unbindButton = GetExternalUnbindButton(provider);
        var statusLabel = GetExternalStatusLabel(provider);

        loadingIndicator.IsVisible = true;
        loadingIndicator.IsRunning = true;
        bindButton.IsVisible = false;
        unbindButton.IsVisible = false;
        statusLabel.Text = AppText.Checking;
    }

    private void SetExternalInfoState(string provider, string message, bool canBind, bool canUnbind)
    {
        var loadingIndicator = GetExternalLoadingIndicator(provider);
        var bindButton = GetExternalBindButton(provider);
        var unbindButton = GetExternalUnbindButton(provider);
        var statusLabel = GetExternalStatusLabel(provider);

        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;
        statusLabel.Text = message;
        bindButton.IsVisible = canBind;
        bindButton.IsEnabled = canBind;
        unbindButton.IsVisible = canUnbind;
        unbindButton.IsEnabled = canUnbind;
    }

    private ActivityIndicator GetExternalLoadingIndicator(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? GitHubLoadingIndicator
            : GoogleLoadingIndicator;
    }

    private Button GetExternalBindButton(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? GitHubBindButton
            : GoogleBindButton;
    }

    private Button GetExternalUnbindButton(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? GitHubUnbindButton
            : GoogleUnbindButton;
    }

    private Label GetExternalStatusLabel(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? GitHubStatusLabel
            : GoogleStatusLabel;
    }

    private static string GetExternalUnbindTitle(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? AppText.UnbindGitHub
            : AppText.UnbindGoogle;
    }

    private static string GetExternalUnbindQuestion(string provider)
    {
        return string.Equals(provider, GitHubProvider, StringComparison.OrdinalIgnoreCase)
            ? AppText.UnbindGitHubQuestion
            : AppText.UnbindGoogleQuestion;
    }

    private static string FormatStorageDiskSpace(SystemHealthSummary health)
    {
        if (health.StorageDiskTotalBytes <= 0)
        {
            return "存储磁盘空间不适用";
        }

        return $"存储磁盘剩余 {FormatBytes(health.StorageDiskAvailableBytes)} / {FormatBytes(health.StorageDiskTotalBytes)}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(bytes, 0);
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }
}
