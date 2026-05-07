namespace PrivateCloudDrive.App.Services;

public sealed class FileContentResult
{
    public required byte[] Content { get; init; }

    public string ContentType { get; init; } = "application/octet-stream";
}
