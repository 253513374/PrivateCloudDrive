using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心IFileCenterBlobStoragePathProvider，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public interface IFileCenterBlobStoragePathProvider
{
    string GetStorageRootPath();
}

/// <summary>
/// 表示文件中心FileCenterBlobStoragePathProvider，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class FileCenterBlobStoragePathProvider : IFileCenterBlobStoragePathProvider, ISingletonDependency
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="FileCenterBlobStoragePathProvider"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterBlobStoragePathProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public string GetStorageRootPath()
    {
        return FileCenterBlobStoragePath.GetFullPath(_configuration);
    }
}
