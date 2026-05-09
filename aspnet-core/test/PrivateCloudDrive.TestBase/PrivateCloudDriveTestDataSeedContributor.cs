using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive;

/// <summary>
/// 表示PrivateCloudDriveTestDataSeedContributor组件，封装对应业务场景的状态或行为。
/// </summary>
public class PrivateCloudDriveTestDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    /// <summary>
    /// 初始化种子数据，确保运行或测试环境具备必要的基础配置。
    /// </summary>
    public Task SeedAsync(DataSeedContext context)
    {
        /* Seed additional test data... */

        return Task.CompletedTask;
    }
}
