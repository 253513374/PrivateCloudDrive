using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 配置PrivateCloudDriveFileCenterApplicationModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainModule),
    typeof(PrivateCloudDriveFileCenterApplicationContractsModule),
    typeof(AbpBlobStoringFileSystemModule)
)]
public class PrivateCloudDriveFileCenterApplicationModule : AbpModule
{
    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var storageRootPath = FileCenterBlobStoragePath.GetFullPath(configuration);

        Configure<FileCenterMediaProcessingOptions>(
            configuration.GetSection("FileCenter:MediaProcessing"));

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
