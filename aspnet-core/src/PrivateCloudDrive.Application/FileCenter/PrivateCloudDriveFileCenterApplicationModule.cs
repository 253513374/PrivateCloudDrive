using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aliyun;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Modularity;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 配置PrivateCloudDriveFileCenterApplicationModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveFileCenterDomainModule),
    typeof(PrivateCloudDriveFileCenterApplicationContractsModule),
    typeof(AbpBlobStoringAliyunModule),
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
        var storageProvider = FileCenterStorageProviderNames.Normalize(configuration["FileCenter:StorageProvider"]);

        Configure<FileCenterMediaProcessingOptions>(
            configuration.GetSection("FileCenter:MediaProcessing"));

        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<FileCenterBlobContainer>(container =>
            {
                if (storageProvider == FileCenterStorageProviderNames.AliyunOss)
                {
                    var aliyunOptions = FileCenterAliyunOssOptions.FromConfiguration(configuration);

                    container.UseAliyun(aliyun =>
                    {
                        aliyun.AccessKeyId = aliyunOptions.AccessKeyId;
                        aliyun.AccessKeySecret = aliyunOptions.AccessKeySecret;
                        aliyun.Endpoint = aliyunOptions.Endpoint;
                        aliyun.RegionId = aliyunOptions.RegionId;
                        aliyun.ContainerName = aliyunOptions.BucketName;
                        aliyun.CreateContainerIfNotExists = aliyunOptions.CreateBucketIfNotExists;
                    });

                    return;
                }

                container.UseFileSystem(fileSystem =>
                {
                    fileSystem.BasePath = storageRootPath;
                    fileSystem.AppendContainerNameToBasePath = true;
                });
            });
        });
    }
}
