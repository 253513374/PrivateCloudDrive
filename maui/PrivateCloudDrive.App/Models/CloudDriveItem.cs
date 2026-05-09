using PrivateCloudDrive.App.Localization;

namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示CloudDriveItem组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed record CloudDriveItem(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Kind,
    string Size,
    string ModifiedAt,
    string Badge,
    string? ContentType,
    bool IsFavorite = false)
{
    public bool IsFolder => Kind == "Folder";

    public bool IsImage => Kind == "Image";

    public bool IsVideo => Kind == "Video";

    public bool CanPreview => IsImage || IsVideo;

    public string DisplayKind => AppText.FileKind(Kind);
}
