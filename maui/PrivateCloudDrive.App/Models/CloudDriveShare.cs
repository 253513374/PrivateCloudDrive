namespace PrivateCloudDrive.App.Models;

public sealed record CloudDriveShare(
    Guid Id,
    Guid FileNodeId,
    string FileName,
    string Token,
    DateTime? ExpirationTime,
    bool AllowDownload,
    bool RequiresPassword);
