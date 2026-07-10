using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 故障诊断清单页 — 展示各服务组件的运行状态和诊断详情，问题类别可展开。
/// </summary>
public partial class FaultDiagnosisPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();

    private bool _apiSectionExpanded;
    private bool _dbSectionExpanded;
    private bool _redisSectionExpanded;
    private bool _storageSectionExpanded;
    private bool _ffmpegSectionExpanded;
    private bool _diagnosticsSectionExpanded;

    public FaultDiagnosisPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDiagnosticsAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadDiagnosticsAsync();
    }

    private async Task LoadDiagnosticsAsync()
    {
        SetLoadingState("正在读取诊断信息...");

        try
        {
            var item = await _apiClient.GetSystemHealthSummaryAsync();

            // Overall summary
            SetHealthDot(OverallDot, item.OverallStatus);
            OverallStatusLabel.Text = item.OverallStatus switch
            {
                SystemHealthStatus.Healthy => "✅ 系统运行正常",
                SystemHealthStatus.Degraded => "⚡ 部分功能降级",
                SystemHealthStatus.Unhealthy => "❌ 系统需要处理",
                _ => "未知"
            };
            OverallDetailLabel.Text = $"API {FormatStatus(item.ApiStatus)} · DB {FormatStatus(item.DatabaseStatus)} · " +
                                      $"Redis {FormatStatus(item.RedisStatus)} · 存储 {FormatStatus(item.StorageStatus)} · " +
                                      $"FFmpeg {FormatStatus(item.FfmpegStatus)} · FFprobe {FormatStatus(item.FfprobeStatus)}";
            GeneratedAtLabel.Text = $"快照时间：{item.GeneratedAt:yyyy-MM-dd HH:mm:ss}";

            // API
            SetHealthDot(ApiStatusDot, item.ApiStatus);
            ApiStatusLabel.Text = FormatStatus(item.ApiStatus);
            ApiSectionContent.IsVisible = _apiSectionExpanded;
            ApiSectionDivider.IsVisible = _apiSectionExpanded;
            ApiDetailLabel.Text = $"API 版本: {item.StorageProvider}\n" +
                                  $"状态: {FormatStatus(item.ApiStatus)}\n" +
                                  $"存储后端: {UserVisibleErrorSanitizer.RedactUserVisibleText(item.StorageProvider, "已隐藏")}";

            // Database
            SetHealthDot(DbStatusDot, item.DatabaseStatus);
            DbStatusLabel.Text = FormatStatus(item.DatabaseStatus);
            DbSectionContent.IsVisible = _dbSectionExpanded;
            DbSectionDivider.IsVisible = _dbSectionExpanded;
            DbDetailLabel.Text = $"连接状态: {FormatStatus(item.DatabaseStatus)}\n" +
                                 $"建议: {(item.DatabaseStatus == SystemHealthStatus.Healthy ? "连接正常，无需处理" : "请检查 PostgreSQL 服务是否运行")}";

            // Redis
            SetHealthDot(RedisStatusDot, item.RedisStatus);
            RedisStatusLabel.Text = FormatStatus(item.RedisStatus);
            RedisSectionContent.IsVisible = _redisSectionExpanded;
            RedisSectionDivider.IsVisible = _redisSectionExpanded;
            RedisDetailLabel.Text = $"连接状态: {FormatStatus(item.RedisStatus)}\n" +
                                    $"建议: {(item.RedisStatus == SystemHealthStatus.Healthy ? "缓存服务正常" : "请检查 Redis 服务是否运行")}";

            // Storage
            SetHealthDot(StorageStatusDot, item.StorageStatus);
            StorageStatusLabel.Text = FormatStatus(item.StorageStatus);
            StorageSectionContent.IsVisible = _storageSectionExpanded;
            StorageSectionDivider.IsVisible = _storageSectionExpanded;
            var diskInfo = item.StorageDiskTotalBytes > 0
                ? $"磁盘: {FormatBytes(item.StorageDiskAvailableBytes)} 可用 / {FormatBytes(item.StorageDiskTotalBytes)} 总计"
                : "磁盘状态: 不适用 (远程存储)";
            StorageDetailLabel.Text = $"存储类型: {UserVisibleErrorSanitizer.RedactUserVisibleText(item.StorageProvider, "已隐藏存储后端类型")}\n" +
                                      $"{diskInfo}\n" +
                                      $"存储位置: {UserVisibleErrorSanitizer.RedactUserVisibleText(item.StorageLocationDescription, "服务器已返回存储位置说明，详细位置已隐藏")}";

            // FFmpeg
            var ffmpegCombined = item.FfmpegStatus == SystemHealthStatus.Unhealthy || item.FfprobeStatus == SystemHealthStatus.Unhealthy
                ? SystemHealthStatus.Unhealthy
                : item.FfmpegStatus == SystemHealthStatus.Degraded || item.FfprobeStatus == SystemHealthStatus.Degraded
                    ? SystemHealthStatus.Degraded
                    : SystemHealthStatus.Healthy;
            SetHealthDot(FfmpegStatusDot, ffmpegCombined);
            FfmpegStatusLabel.Text = FormatStatus(ffmpegCombined);
            FfmpegSectionContent.IsVisible = _ffmpegSectionExpanded;
            FfmpegSectionDivider.IsVisible = _ffmpegSectionExpanded;
            FfmpegDetailLabel.Text = $"FFmpeg: {FormatStatus(item.FfmpegStatus)}\n" +
                                     $"FFprobe: {FormatStatus(item.FfprobeStatus)}\n" +
                                     $"建议: {(ffmpegCombined == SystemHealthStatus.Healthy ? "媒体处理工具正常" : "请检查 FFmpeg/FFprobe 是否已安装并配置正确路径")}";

            // Diagnostics
            var hasDiagnostics = item.Diagnostics.Count > 0;
            SetHealthDot(DiagnosticsStatusDot, hasDiagnostics ? SystemHealthStatus.Unhealthy : SystemHealthStatus.Healthy);
            DiagnosticsCountLabel.Text = hasDiagnostics ? $"{item.Diagnostics.Count} 项" : "无";
            DiagnosticsSectionContent.IsVisible = _diagnosticsSectionExpanded;
            DiagnosticsSectionDivider.IsVisible = _diagnosticsSectionExpanded;
            DiagnosticsDetailLabel.Text = hasDiagnostics
                ? string.Join("\n\n", item.Diagnostics.Select(d => $"• {UserVisibleErrorSanitizer.RedactUserVisibleText(d, "诊断详情已隐藏")}"))
                : "无待处理的诊断信息。";

            SetIdleState();
        }
        catch (Exception exception)
        {
            SetErrorState($"无法加载诊断信息。{UserVisibleErrorSanitizer.ForSystemHealth(exception)}");
        }
    }

    private void OnToggleApiSection(object? sender, TappedEventArgs e)
    {
        _apiSectionExpanded = !_apiSectionExpanded;
        ApiSectionContent.IsVisible = _apiSectionExpanded;
        ApiSectionDivider.IsVisible = _apiSectionExpanded;
    }

    private void OnToggleDbSection(object? sender, TappedEventArgs e)
    {
        _dbSectionExpanded = !_dbSectionExpanded;
        DbSectionContent.IsVisible = _dbSectionExpanded;
        DbSectionDivider.IsVisible = _dbSectionExpanded;
    }

    private void OnToggleRedisSection(object? sender, TappedEventArgs e)
    {
        _redisSectionExpanded = !_redisSectionExpanded;
        RedisSectionContent.IsVisible = _redisSectionExpanded;
        RedisSectionDivider.IsVisible = _redisSectionExpanded;
    }

    private void OnToggleStorageSection(object? sender, TappedEventArgs e)
    {
        _storageSectionExpanded = !_storageSectionExpanded;
        StorageSectionContent.IsVisible = _storageSectionExpanded;
        StorageSectionDivider.IsVisible = _storageSectionExpanded;
    }

    private void OnToggleFfmpegSection(object? sender, TappedEventArgs e)
    {
        _ffmpegSectionExpanded = !_ffmpegSectionExpanded;
        FfmpegSectionContent.IsVisible = _ffmpegSectionExpanded;
        FfmpegSectionDivider.IsVisible = _ffmpegSectionExpanded;
    }

    private void OnToggleDiagnosticsSection(object? sender, TappedEventArgs e)
    {
        _diagnosticsSectionExpanded = !_diagnosticsSectionExpanded;
        DiagnosticsSectionContent.IsVisible = _diagnosticsSectionExpanded;
        DiagnosticsSectionDivider.IsVisible = _diagnosticsSectionExpanded;
    }

    private static void SetHealthDot(Microsoft.Maui.Controls.Shapes.Ellipse dot, SystemHealthStatus status)
    {
        dot.Fill = new SolidColorBrush(status switch
        {
            SystemHealthStatus.Healthy => Color.FromArgb("#00C853"),
            SystemHealthStatus.Degraded => Color.FromArgb("#FF9800"),
            SystemHealthStatus.Unhealthy => Color.FromArgb("#D32F2F"),
            _ => Colors.Gray
        });
    }

    private static string FormatStatus(SystemHealthStatus status)
    {
        return status switch
        {
            SystemHealthStatus.Healthy => "✅ 正常",
            SystemHealthStatus.Degraded => "⚠️ 降级",
            SystemHealthStatus.Unhealthy => "❌ 异常",
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
}
