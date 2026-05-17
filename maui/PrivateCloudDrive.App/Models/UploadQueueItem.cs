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

    public string StatusText => AppText.UploadStatus(Status);

    public string ProgressText => Status == UploadQueueStatus.Completed
        ? "100%"
        : $"{Progress:P0}";

    public bool IsFailed => Status == UploadQueueStatus.Failed;

    public bool IsCompleted => Status == UploadQueueStatus.Completed;

    public bool CanRetry => Status == UploadQueueStatus.Failed;

    public string FailureHint => IsFailed
        ? LastAttemptAt.HasValue
            ? $"失败任务已保留，可在确认服务器/网络恢复后重试。上次尝试：{LastAttemptAt.Value.LocalDateTime:HH:mm}"
            : "失败任务已保留，可在确认服务器/网络恢复后重试。"
        : string.Empty;

    /// <summary>
    /// 执行MarkUploading操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public void MarkUploading()
    {
        LastAttemptAt = DateTimeOffset.Now;
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

    /// <summary>
    /// 执行MarkCompleted操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public void MarkCompleted()
    {
        Progress = 1;
        ErrorMessage = null;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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
