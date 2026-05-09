namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示RemoteFileContentSource组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed record RemoteFileContentSource(
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers);
