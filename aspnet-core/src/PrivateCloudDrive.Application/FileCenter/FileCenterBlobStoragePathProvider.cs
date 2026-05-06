using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterBlobStoragePathProvider
{
    string GetStorageRootPath();
}

public class FileCenterBlobStoragePathProvider : IFileCenterBlobStoragePathProvider, ISingletonDependency
{
    private readonly IConfiguration _configuration;

    public FileCenterBlobStoragePathProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetStorageRootPath()
    {
        return FileCenterBlobStoragePath.GetFullPath(_configuration);
    }
}
