namespace PrivateCloudDrive.App.Models;

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
}
