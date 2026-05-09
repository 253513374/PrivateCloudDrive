using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示MediaLibraryItem组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed class MediaLibraryItem : INotifyPropertyChanged
{
    private readonly CloudDriveItem? _item;
    private readonly MediaTimelineItem? _timelineItem;
    private ImageSource? _thumbnailSource;

    /// <summary>
    /// 初始化 <see cref="MediaLibraryItem"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MediaLibraryItem(CloudDriveItem item)
    {
        _item = item;
    }

    public MediaLibraryItem(MediaTimelineItem item)
    {
        _timelineItem = item;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CloudDriveItem? Item => _item;

    public MediaTimelineItem? TimelineItem => _timelineItem;

    public Guid Id => _timelineItem?.Id ?? _item!.Id;

    public string Name => _timelineItem?.Name ?? _item!.Name;

    public string Kind => _timelineItem == null
        ? _item!.Kind
        : _timelineItem.MediaType == MediaAssetMediaType.Video
            ? "Video"
            : "Image";

    public string DisplayKind => _timelineItem == null
        ? _item!.DisplayKind
        : _timelineItem.MediaType == MediaAssetMediaType.Video
            ? "视频"
            : "图片";

    public string Size => _timelineItem == null ? _item!.Size : FormatSize(_timelineItem.Size);

    public string ModifiedAt => _timelineItem == null
        ? _item!.ModifiedAt
        : _timelineItem.TimelineTime.ToString("M月d日 HH:mm");

    public string Badge => _timelineItem == null
        ? _item!.Badge
        : _timelineItem.MediaType == MediaAssetMediaType.Video
            ? "VID"
            : "IMG";

    public bool IsFavorite => _timelineItem?.IsFavorite ?? _item!.IsFavorite;

    public string ProcessStatusText => _timelineItem == null
        ? string.Empty
        : _timelineItem.ProcessStatus switch
        {
            MediaAssetProcessStatus.Pending => "等待处理",
            MediaAssetProcessStatus.Processing => "处理中",
            MediaAssetProcessStatus.Failed => "处理失败",
            MediaAssetProcessStatus.Completed => "已完成",
            _ => "未知"
        };

    public bool ShowProcessStatus => _timelineItem != null &&
                                     _timelineItem.ProcessStatus != MediaAssetProcessStatus.Completed;

    public string DurationText => FormatDuration(_timelineItem?.DurationMilliseconds);

    public string SecondaryText => string.IsNullOrWhiteSpace(DurationText)
        ? Size
        : $"{DurationText} · {Size}";

    public ImageSource? ThumbnailSource
    {
        get => _thumbnailSource;
        set
        {
            if (_thumbnailSource == value)
            {
                return;
            }

            _thumbnailSource = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasThumbnail));
            OnPropertyChanged(nameof(ShowBadge));
        }
    }

    public bool HasThumbnail => _thumbnailSource != null;

    public bool ShowBadge => !HasThumbnail;

    public bool IsVideo => Kind == "Video";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{size} {units[unitIndex]}" : $"{value:0.##} {units[unitIndex]}";
    }

    private static string FormatDuration(long? milliseconds)
    {
        if (!milliseconds.HasValue || milliseconds <= 0)
        {
            return string.Empty;
        }

        var duration = TimeSpan.FromMilliseconds(milliseconds.Value);
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes}:{duration.Seconds:00}";
    }
}
