using System.Collections.ObjectModel;

namespace PrivateCloudDrive.App.Models;

public sealed class MediaTimelineGroup : ObservableCollection<MediaLibraryItem>
{
    public MediaTimelineGroup(DateTime month, IEnumerable<MediaLibraryItem> items)
        : base(items)
    {
        Month = month;
    }

    public DateTime Month { get; }

    public string Name => Month.ToString("yyyy 年 M 月");

    public string CountText => $"{Count} 项";
}
