using System.IO;
using Microsoft.Extensions.Configuration;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心FileCenterBlobStoragePath，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public static class FileCenterBlobStoragePath
{
    private const string DefaultStorageRootPath = "App_Data/FileCenter";

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public static string GetFullPath(IConfiguration configuration)
    {
        var configuredPath = configuration["FileCenter:StorageRootPath"];
        var storageRootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? DefaultStorageRootPath
            : configuredPath;

        return Path.IsPathRooted(storageRootPath)
            ? storageRootPath
            : Path.GetFullPath(storageRootPath, Directory.GetCurrentDirectory());
    }
}
