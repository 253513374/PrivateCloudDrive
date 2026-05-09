using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示MediaLibraryItem组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed class MediaLibraryItem : INotifyPropertyChanged
{
    private ImageSource? _thumbnailSource;

    /// <summary>
    /// 初始化 <see cref="MediaLibraryItem"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
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
