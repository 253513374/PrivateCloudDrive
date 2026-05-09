namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示FileContentResult操作结果，用于向调用方返回处理状态和必要业务信息。
/// </summary>
public sealed class FileContentResult
{
    public required byte[] Content { get; init; }

    public string ContentType { get; init; } = "application/octet-stream";
}
