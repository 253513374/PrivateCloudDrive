using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivateCloudDrive.App.Models;

public sealed class MediaLibraryItem : INotifyPropertyChanged
{
    private ImageSource? _thumbnailSource;

    public MediaLibraryItem(CloudDriveItem item)
    {
        Item = item;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CloudDriveItem Item { get; }

    public Guid Id => Item.Id;

    public string Name => Item.Name;

    public string Kind => Item.Kind;

    public string DisplayKind => Item.DisplayKind;

    public string Size => Item.Size;

    public string ModifiedAt => Item.ModifiedAt;

    public string Badge => Item.Badge;

    public bool IsFavorite => Item.IsFavorite;

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
