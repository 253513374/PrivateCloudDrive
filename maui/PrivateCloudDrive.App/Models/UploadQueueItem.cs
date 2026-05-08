using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace PrivateCloudDrive.App.Models;

public sealed class UploadQueueItem : INotifyPropertyChanged
{
    private double _progress;
    private UploadQueueStatus _status = UploadQueueStatus.Waiting;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UploadQueueItem(FileResult file, string targetPath)
    {
        File = file;
        FileName = file.FileName;
        TargetPath = targetPath;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public FileResult File { get; }

    public string FileName { get; }

    public string TargetPath { get; }

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

    public string StatusText => Status switch
    {
        UploadQueueStatus.Waiting => "Waiting",
        UploadQueueStatus.Uploading => "Uploading",
        UploadQueueStatus.Completed => "Completed",
        UploadQueueStatus.Failed => "Failed",
        _ => "Unknown"
    };

    public string ProgressText => Status == UploadQueueStatus.Completed
        ? "100%"
        : $"{Progress:P0}";

    public bool IsFailed => Status == UploadQueueStatus.Failed;

    public bool IsCompleted => Status == UploadQueueStatus.Completed;

    public void MarkUploading()
    {
        ErrorMessage = null;
        Status = UploadQueueStatus.Uploading;
    }

    public void UpdateProgress(double value)
    {
        Progress = value;
    }

    public void MarkCompleted()
    {
        Progress = 1;
        ErrorMessage = null;
        Status = UploadQueueStatus.Completed;
    }

    public void MarkFailed(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = UploadQueueStatus.Failed;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum UploadQueueStatus
{
    Waiting,
    Uploading,
    Completed,
    Failed
}
