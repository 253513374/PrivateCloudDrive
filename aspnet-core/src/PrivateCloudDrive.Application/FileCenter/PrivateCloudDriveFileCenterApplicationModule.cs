using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainModule),
    typeof(PrivateCloudDriveFileCenterApplicationContractsModule),
    typeof(AbpBlobStoringFileSystemModule)
)]
public class PrivateCloudDriveFileCenterApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var storageRootPath = FileCenterBlobStoragePath.GetFullPath(configuration);

        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<FileCenterBlobContainer>(container =>
            {
                container.UseFileSystem(fileSystem =>
                {
                    fileSystem.BasePath = storageRootPath;
                    fileSystem.AppendContainerNameToBasePath = true;
                });
            });
        });
    }
}
