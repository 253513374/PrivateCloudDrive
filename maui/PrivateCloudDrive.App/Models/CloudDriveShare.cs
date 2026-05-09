namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示CloudDriveShare组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed record CloudDriveShare(
    Guid Id,
    Guid FileNodeId,
    string FileName,
    string Token,
    DateTime? ExpirationTime,
    bool AllowDownload,
    bool RequiresPassword);
