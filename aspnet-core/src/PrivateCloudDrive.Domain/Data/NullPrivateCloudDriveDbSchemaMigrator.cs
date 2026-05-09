using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.Data;

/* This is used if database provider does't define
 * IPrivateCloudDriveDbSchemaMigrator implementation.
 */
/// <summary>
/// 表示NullPrivateCloudDriveDbSchemaMigrator组件，封装对应业务场景的状态或行为。
/// </summary>
public class NullPrivateCloudDriveDbSchemaMigrator : IPrivateCloudDriveDbSchemaMigrator, ITransientDependency
{
    /// <summary>
    /// 执行数据库迁移流程，使当前数据库结构与应用模型保持一致。
    /// </summary>
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
