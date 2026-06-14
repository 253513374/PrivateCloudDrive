using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;
using PrivateCloudDrive.App.Localization;

namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示UploadQueueItem组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed class UploadQueueItem : INotifyPropertyChanged
{
    private double _progress;
    private UploadQueueStatus _status = UploadQueueStatus.Waiting;
    private string? _errorMessage;
    private DateTimeOffset? _lastAttemptAt;
    private DateTimeOffset? _completedAt;
    private UploadTransferProgress? _serverProgress;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 初始化 <see cref="UploadQueueItem"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public UploadQueueItem(FileResult file, string targetPath, Guid? targetFolderId)
    {
        File = file;
        FileName = file.FileName;
        TargetPath = targetPath;
        TargetFolderId = targetFolderId;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public FileResult File { get; }

    public string FileName { get; }

    public string TargetPath { get; }

    public Guid? TargetFolderId { get; }

    public double Progress
    {
        get => _progress;
        private set
        {
            if (Math.Abs(_progress - value) < 0.001)
            {
                return;
            }

            _progress = Math.Clamp(value, 0, 1);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public UploadQueueStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(CanRetry));
            OnPropertyChanged(nameof(FailureHint));
            OnPropertyChanged(nameof(ServerStateText));
            OnPropertyChanged(nameof(RecoveryActionText));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(FailureHint));
        }
    }

    public DateTimeOffset? LastAttemptAt
    {
        get => _lastAttemptAt;
        private set
        {
            if (_lastAttemptAt == value)
            {
                return;
            }

            _lastAttemptAt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FailureHint));
        }
    }

    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        private set
        {
            if (_completedAt == value)
            {
                return;
            }

            _completedAt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CompletedAtText));
        }
    }

    public UploadTransferProgress? ServerProgress
    {
        get => _serverProgress;
        private set
        {
            _serverProgress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ServerStateText));
            OnPropertyChanged(nameof(RecoveryActionText));
            OnPropertyChanged(nameof(UploadedBytesText));
            OnPropertyChanged(nameof(UploadedChunksText));
            OnPropertyChanged(nameof(FailureHint));
            OnPropertyChanged(nameof(CanRetry));
        }
    }

    public string StatusText => AppText.UploadStatus(Status);

    public string ProgressText => Status == UploadQueueStatus.Completed
        ? "100%"
        : $"{Progress:P0}";

    public bool IsFailed => Status == UploadQueueStatus.Failed;

    public bool IsCompleted => Status == UploadQueueStatus.Completed;

    public bool CanRetry => Status == UploadQueueStatus.Failed && (ServerProgress?.IsRetryable ?? true);

    public string CompletedAtText => CompletedAt.HasValue
        ? $"完成时间：{CompletedAt.Value.LocalDateTime:MM-dd HH:mm}"
        : string.Empty;

    public string ServerStateText => ServerProgress is null
        ? "服务器状态：等待创建上传会话"
        : $"服务器状态：{GetStatusReasonText(ServerProgress.StatusReason)}";

    public string RecoveryActionText => ServerProgress is null
        ? "恢复建议：等待上传开始"
        : $"恢复建议：{GetNextActionText(ServerProgress.NextAction, ServerProgress.StatusReason, ServerProgress.FailureReason)}";

    public string UploadedBytesText => ServerProgress is null
        ? "已上传：--"
        : $"已上传：{FormatBytes(ServerProgress.UploadedBytes)}";

    public string UploadedChunksText => ServerProgress is null
        ? "分片：--"
        : $"分片：{ServerProgress.UploadedChunkCount} 个";

    public string FailureHint
    {
        get
        {
            if (!IsFailed)
            {
                return string.Empty;
            }

            var action = ServerProgress is null
                ? "确认服务器/网络恢复后重试。"
                : GetNextActionText(ServerProgress.NextAction, ServerProgress.StatusReason, ServerProgress.FailureReason);
            var retry = (ServerProgress?.IsRetryable ?? true) ? "可继续重试备份。" : "当前服务端标记为不可重试。";
            var time = LastAttemptAt.HasValue
                ? $" 上次尝试：{LastAttemptAt.Value.LocalDateTime:HH:mm}"
                : string.Empty;
            return $"失败任务已保留，{action}{retry}{time}";
        }
    }

    /// <summary>
    /// 执行MarkUploading操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public void MarkUploading()
    {
        LastAttemptAt = DateTimeOffset.Now;
        CompletedAt = null;
        Progress = 0;
        ErrorMessage = null;
        Status = UploadQueueStatus.Uploading;
    }

    /// <summary>
    /// 更新现有业务资源，并保持跨层数据和领域状态一致。
    /// </summary>
    public void UpdateProgress(double value)
    {
        Progress = value;
    }

    public void ApplyServerProgress(UploadTransferProgress progress)
    {
        ServerProgress = progress;
        if (progress.ProgressPercent.HasValue)
        {
            Progress = (double)(progress.ProgressPercent.Value / 100m);
        }
        else if (progress.TotalBytes > 0)
        {
            Progress = progress.UploadedBytes / (double)progress.TotalBytes;
        }

        if (IsCompletedStatus(progress.StatusReason) || IsOpenFileAction(progress.NextAction))
        {
            MarkCompleted();
            return;
        }

        if (IsCancelled(progress.StatusReason, progress.FailureReason))
        {
            MarkFailed(GetNextActionText(progress.NextAction, progress.StatusReason, progress.FailureReason));
        }
    }

    /// <summary>
    /// 执行MarkCompleted操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public void MarkCompleted()
    {
        Progress = 1;
        ErrorMessage = null;
        CompletedAt = DateTimeOffset.Now;
        Status = UploadQueueStatus.Completed;
    }

    /// <summary>
    /// 执行MarkFailed操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public void MarkFailed(string errorMessage)
    {
        LastAttemptAt ??= DateTimeOffset.Now;
        ErrorMessage = errorMessage;
        Status = UploadQueueStatus.Failed;
    }

    public static string GetStatusReasonText(string? statusReason)
    {
        return statusReason switch
        {
            "WaitingForChunks" => "等待剩余分片",
            "Completed" => "已完成",
            "Cancelled" => "已取消",
            "Unknown" or null or "" => "未知状态",
            _ => $"未知状态：{statusReason}"
        };
    }

    public static string GetNextActionText(string? nextAction, string? statusReason = null, string? failureReason = null)
    {
        if (IsCancelled(statusReason, failureReason) || nextAction == "StartNewUploadSession")
        {
            return "重新开始备份";
        }

        return nextAction switch
        {
            "UploadMissingChunks" => "继续上传缺失分片",
            "OpenFile" => "打开已完成文件",
            "StartNewUploadSession" => "重新开始备份",
            null or "" => "等待客户端兼容处理",
            _ => $"等待客户端兼容处理：{nextAction}"
        };
    }

    private static bool IsCompletedStatus(string? statusReason)
    {
        return string.Equals(statusReason, "Completed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOpenFileAction(string? nextAction)
    {
        return string.Equals(nextAction, "OpenFile", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCancelled(string? statusReason, string? failureReason)
    {
        return string.Equals(statusReason, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(failureReason, "PrivateCloudDrive:FileCenter:000033", StringComparison.OrdinalIgnoreCase) ||
               (failureReason?.Contains("PrivateCloudDrive:FileCenter:000033", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = Math.Max(0, bytes);
        var value = (double)size;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{size} {units[unit]}" : $"{value:0.#} {units[unit]}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class UploadTransferProgress
{
    public UploadTransferProgress(
        double progress,
        int uploadedChunkCount = 0,
        long uploadedBytes = 0,
        long totalBytes = 0,
        decimal? progressPercent = null,
        bool isRetryable = true,
        string? statusReason = null,
        string? failureReason = null,
        string? nextAction = null)
    {
        Progress = Math.Clamp(progress, 0, 1);
        UploadedChunkCount = Math.Max(0, uploadedChunkCount);
        UploadedBytes = Math.Max(0, uploadedBytes);
        TotalBytes = Math.Max(0, totalBytes);
        ProgressPercent = progressPercent;
        IsRetryable = isRetryable;
        StatusReason = statusReason;
        FailureReason = failureReason;
        NextAction = nextAction;
    }

    public double Progress { get; }

    public int UploadedChunkCount { get; }

    public long UploadedBytes { get; }

    public long TotalBytes { get; }

    public decimal? ProgressPercent { get; }

    public bool IsRetryable { get; }

    public string? StatusReason { get; }

    public string? FailureReason { get; }

    public string? NextAction { get; }
}

/// <summary>
/// 表示UploadQueueStatus组件，封装对应业务场景的状态或行为。
/// </summary>
public enum UploadQueueStatus
{
    Waiting,
    Uploading,
    Completed,
    Failed
}
