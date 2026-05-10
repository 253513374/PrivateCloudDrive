using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivateCloudDrive.App.Models;

public sealed class SelectableMediaLibraryItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableMediaLibraryItem(MediaLibraryItem media)
    {
        Media = media;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MediaLibraryItem Media { get; }

    public Guid Id => Media.Id;

    public string Name => Media.Name;

    public string Badge => Media.Badge;

    public string SecondaryText => Media.SecondaryText;

    public string TimelineMetaText => Media.TimelineMetaText;

    public bool HasThumbnail => Media.HasThumbnail;

    public bool ShowBadge => Media.ShowBadge;

    public bool IsVideo => Media.IsVideo;

    public bool ShowVideoDuration => Media.ShowVideoDuration;

    public string DurationText => Media.DurationText;

    public ImageSource? ThumbnailSource
    {
        get => Media.ThumbnailSource;
        set
        {
            Media.ThumbnailSource = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasThumbnail));
            OnPropertyChanged(nameof(ShowBadge));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionText));
        }
    }

    public string SelectionText => IsSelected ? "已选" : string.Empty;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
