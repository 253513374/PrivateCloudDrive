using PrivateCloudDrive.App.Localization;

namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 表示CloudOperationLog组件，封装对应业务场景的状态或行为。
/// </summary>
public sealed record CloudOperationLog(
    Guid Id,
    DateTime Time,
    string Source,
    string Action,
    string Result,
    string? UserName,
    string? ClientIpAddress,
    int? HttpStatusCode,
    string Summary)
{
    public string DisplayTime => Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string DisplayUser => string.IsNullOrWhiteSpace(UserName)
        ? AppText.UnknownUser
        : UserName;

    public string DisplayStatus => HttpStatusCode.HasValue
        ? $"{Result} HTTP {HttpStatusCode.Value}"
        : Result;
}
