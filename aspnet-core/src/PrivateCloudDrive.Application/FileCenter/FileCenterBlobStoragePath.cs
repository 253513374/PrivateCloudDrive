using System.IO;
using Microsoft.Extensions.Configuration;

namespace PrivateCloudDrive.FileCenter;

public static class FileCenterBlobStoragePath
{
    private const string DefaultStorageRootPath = "App_Data/FileCenter";

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
