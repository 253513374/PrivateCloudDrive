namespace PrivateCloudDrive.App.Services;

public sealed record RemoteFileContentSource(
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers);
