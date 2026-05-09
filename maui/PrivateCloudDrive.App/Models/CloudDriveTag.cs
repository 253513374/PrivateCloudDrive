namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示CloudDriveTag组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed record CloudDriveTag(
    Guid Id,
    string Name,
    string? Color);
