namespace PrivateCloudDrive.App.Models;

/// <summary>
/// 文件列表查询条件。
/// </summary>
public sealed class CloudDriveQueryOptions
{
    public string? SearchKeyword { get; init; }

    public string SearchScope { get; init; } = "CurrentFolder";

    public string? NodeType { get; init; }

    public string? MediaType { get; init; }

    public string? Sorting { get; init; }
}
