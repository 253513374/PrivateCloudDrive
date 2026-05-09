namespace PrivateCloudDrive.App.Models;

public sealed record MediaAlbum(
    Guid Id,
    string Name,
    string? Description,
    Guid? CoverFileNodeId,
    Guid? CoverThumbnailBlobObjectId,
    int ItemsCount,
    DateTime CreationTime,
    DateTime? LastModificationTime);
