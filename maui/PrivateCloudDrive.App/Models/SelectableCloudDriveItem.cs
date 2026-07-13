using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 包装 CloudDriveItem，添加 IsSelected 属性以支持多选模式下的复选框绑定。
/// 遵循项目中已有的 SelectableMediaLibraryItem 模式。
/// </summary>
public sealed class SelectableCloudDriveItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableCloudDriveItem(CloudDriveItem item)
    {
        Item = item;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CloudDriveItem Item { get; }

    public Guid Id => Item.Id;

    public Guid? ParentId => Item.ParentId;

    public string Name => Item.Name;

    public string Kind => Item.Kind;

    public string Size => Item.Size;

    public string ModifiedAt => Item.ModifiedAt;

    public string Badge => Item.Badge;

    public string? ContentType => Item.ContentType;

    public bool IsFavorite => Item.IsFavorite;

    public bool IsFolder => Item.IsFolder;

    public bool IsImage => Item.IsImage;

    public bool IsVideo => Item.IsVideo;

    public bool CanPreview => Item.CanPreview;

    public string DisplayKind => Item.DisplayKind;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
