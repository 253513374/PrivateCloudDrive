using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// Storage usage screen backed by the current private backup server.
/// </summary>
public partial class StorageUsagePage : ContentPage
{
    private readonly IAuthService _authService = AppServices.GetRequiredService<IAuthService>();
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    public StorageUsagePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStorageStateAsync();
    }

    private async Task LoadStorageStateAsync()
    {
        SetLoadingState();

        try
        {
            var isSignedIn = await _authService.IsSignedInAsync();
            if (!isSignedIn)
            {
                SetSignedOutState();
                return;
            }

            var usageTask = _apiClient.GetStorageUsageAsync();
            var healthTask = _apiClient.GetSystemHealthSummaryAsync();
            await Task.WhenAll(usageTask, healthTask);

            SetUsageState(usageTask.Result, healthTask.Result);
        }
        catch (AuthSessionExpiredException)
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//login", true);
        }
        catch (Exception exception)
        {
            SetErrorState(UserVisibleErrorSanitizer.ForStorage(exception));
        }
    }

    private void SetLoadingState()
    {
        UsagePercentLabel.Text = "--";
        UsageAmountLabel.Text = "正在读取容量";
        UsageDetailLabel.Text = "连接当前私有备份服务器，读取真实容量和健康状态。";
        UsageProgressBar.Progress = 0;
        StorageProviderLabel.Text = "后端存储：读取中";
        DiskSpaceLabel.Text = "磁盘空间：读取中";
        QuotaStateLabel.Text = "容量策略：读取中";
        StorageHealthLabel.Text = "健康状态：读取中";
        StorageLocationLabel.Text = "存储位置：读取中";
        BackupScopeLabel.Text = "恢复边界：读取中";
        PrivacyBoundaryLabel.Text = "隐私边界：读取中";
        StorageSuggestionLabel.Text = "正在检查私有备份空间。";
    }

    private void SetSignedOutState()
    {
        UsagePercentLabel.Text = "--";
        UsageAmountLabel.Text = "需要登录";
        UsageDetailLabel.Text = "登录后可查看当前服务器的真实容量、磁盘空间和健康状态。";
        UsageProgressBar.Progress = 0;
        StorageProviderLabel.Text = "后端存储：不可用";
        DiskSpaceLabel.Text = "磁盘空间：不可用";
        QuotaStateLabel.Text = "容量策略：不可用";
        StorageHealthLabel.Text = "健康状态：不可用";
        StorageLocationLabel.Text = "存储位置：登录后读取";
        BackupScopeLabel.Text = "恢复边界：登录后读取";
        PrivacyBoundaryLabel.Text = "隐私边界：登录后读取";
        StorageSuggestionLabel.Text = "请先登录当前私有备份服务器。";
    }

    private void SetUsageState(StorageUsage usage, SystemHealthSummary health)
    {
        var percent = usage.IsQuotaConfigured
            ? Math.Clamp((double)usage.UsagePercent, 0, 100)
            : CalculatePercent(health.StorageDiskTotalBytes - health.StorageDiskAvailableBytes, health.StorageDiskTotalBytes);

        UsagePercentLabel.Text = usage.IsQuotaConfigured ? $"{percent:0.#}%" : "可信";
        UsageAmountLabel.Text = usage.IsQuotaConfigured
            ? $"{FormatBytes(usage.UsedBytes)} / {FormatBytes(usage.QuotaBytes)}"
            : $"{FormatBytes(usage.UsedBytes)} 已备份";
        UsageDetailLabel.Text = usage.IsQuotaConfigured
            ? $"剩余 {FormatBytes(usage.RemainingBytes)} · 当前私有备份服务器 {FormatHealthStatus(health.OverallStatus)}"
            : $"未配置容量上限 · 当前私有备份服务器 {FormatHealthStatus(health.OverallStatus)}";
        UsageProgressBar.Progress = usage.IsQuotaConfigured ? percent / 100 : 0;

        StorageProviderLabel.Text = $"后端存储：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.StorageProvider, "当前存储后端")} · {FormatHealthStatus(health.StorageStatus)}";
        DiskSpaceLabel.Text = health.StorageDiskTotalBytes > 0
            ? $"磁盘空间：剩余 {FormatBytes(health.StorageDiskAvailableBytes)} / {FormatBytes(health.StorageDiskTotalBytes)}"
            : "磁盘空间：当前存储后端未提供磁盘容量";
        QuotaStateLabel.Text = usage.IsQuotaConfigured
            ? $"容量策略：已配置配额，使用 {percent:0.#}%"
            : FormatUnboundedQuotaPolicy(health);
        StorageHealthLabel.Text = health.Diagnostics.Count == 0
            ? $"健康状态：{FormatHealthStatus(health.OverallStatus)} · 更新时间 {health.GeneratedAt:yyyy-MM-dd HH:mm}"
            : $"健康状态：{FormatHealthStatus(health.OverallStatus)} · {string.Join("；", health.Diagnostics.Take(3).Select(item => UserVisibleErrorSanitizer.RedactUserVisibleText(item, "诊断详情已隐藏")))}";
        StorageLocationLabel.Text = string.IsNullOrWhiteSpace(health.StorageLocationDescription)
            ? "存储位置：服务器未返回可展示说明"
            : $"存储位置：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.StorageLocationDescription, "服务器已返回存储位置说明，详细位置已隐藏")}";
        BackupScopeLabel.Text = string.IsNullOrWhiteSpace(health.BackupScopeDescription)
            ? "恢复边界：请同时备份数据库、文件存储和部署配置；仅备份手机 App 本机数据不能恢复服务器文件。"
            : $"恢复边界：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.BackupScopeDescription, "服务器已返回恢复边界说明，敏感细节已隐藏")}";
        PrivacyBoundaryLabel.Text = string.IsNullOrWhiteSpace(health.PrivacyBoundaryDescription)
            ? "隐私边界：文件保存到当前连接的私有后端，分享链接会扩大访问边界。"
            : $"隐私边界：{UserVisibleErrorSanitizer.RedactUserVisibleText(health.PrivacyBoundaryDescription, "服务器已返回隐私边界说明，敏感细节已隐藏")}";

        StorageSuggestionLabel.Text = health.OverallStatus == SystemHealthStatus.Healthy
            ? "私有备份服务器运行正常，可以继续备份照片、视频和本机文件。"
            : "服务器存在降级或异常，请先检查系统健康详情，再继续大批量备份。";
    }

    private void SetErrorState(string message)
    {
        UsagePercentLabel.Text = "--";
        UsageAmountLabel.Text = "无法读取容量";
        UsageDetailLabel.Text = string.IsNullOrWhiteSpace(message)
            ? "请确认当前私有备份服务器在线后重试。"
            : "请确认当前私有备份服务器在线，或稍后在“我的”页重新进入存储用量。";
        UsageProgressBar.Progress = 0;
        StorageProviderLabel.Text = "后端存储：读取失败";
        DiskSpaceLabel.Text = "磁盘空间：读取失败";
        QuotaStateLabel.Text = "容量策略：读取失败";
        StorageHealthLabel.Text = "健康状态：读取失败";
        StorageLocationLabel.Text = "存储位置：读取失败，未展示服务器内部路径或存储标识";
        BackupScopeLabel.Text = "恢复边界：读取失败，请按部署文档保守备份数据库、文件存储和部署密钥配置。";
        PrivacyBoundaryLabel.Text = "隐私边界：读取失败，当前页面不会展示连接串、密钥、Token 或存储内部标识。";
        StorageSuggestionLabel.Text = "请确认当前私有备份服务器在线，并在“我的”页重试。";
    }

    private static string FormatUnboundedQuotaPolicy(SystemHealthSummary health)
    {
        if (string.Equals(health.StorageProvider, "AliyunOss", StringComparison.OrdinalIgnoreCase))
        {
            return "容量策略：未配置容量上限，容量受对象存储配额和计费策略影响";
        }

        return health.StorageDiskTotalBytes > 0
            ? "容量策略：未配置容量上限，按服务器磁盘可用空间备份"
            : "容量策略：未配置容量上限，请以当前存储后端实际限制为准";
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnAiCleanupClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("智能整理", "当前版本先展示真实容量和健康状态；智能清理会在后续版本接入媒体分析和回收站建议。", "知道了");
    }

    private static double CalculatePercent(long usedBytes, long totalBytes)
    {
        if (totalBytes <= 0)
        {
            return 0;
        }

        return Math.Clamp(usedBytes * 100d / totalBytes, 0, 100);
    }

    private static string FormatHealthStatus(SystemHealthStatus status)
    {
        return status switch
        {
            SystemHealthStatus.Healthy => "正常",
            SystemHealthStatus.Degraded => "降级",
            SystemHealthStatus.Unhealthy => "异常",
            _ => "未知"
        };
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
