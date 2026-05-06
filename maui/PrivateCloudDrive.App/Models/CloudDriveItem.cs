namespace PrivateCloudDrive.App.Models;

public sealed record CloudDriveItem(
    string Name,
    string Kind,
    string Size,
    string ModifiedAt,
    string Badge);
