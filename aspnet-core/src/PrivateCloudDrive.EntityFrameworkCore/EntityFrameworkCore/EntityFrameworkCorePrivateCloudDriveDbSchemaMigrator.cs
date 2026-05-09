using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrivateCloudDrive.Data;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.EntityFrameworkCore;

/// <summary>
/// 表示EntityFrameworkCorePrivateCloudDriveDbSchemaMigrator组件，封装对应业务场景的状态或行为。
/// </summary>
public class EntityFrameworkCorePrivateCloudDriveDbSchemaMigrator
    : IPrivateCloudDriveDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化 <see cref="EntityFrameworkCorePrivateCloudDriveDbSchemaMigrator"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EntityFrameworkCorePrivateCloudDriveDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 执行数据库迁移流程，使当前数据库结构与应用模型保持一致。
    /// </summary>
    public async Task MigrateAsync()
    {
        /* We intentionally resolve the PrivateCloudDriveDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<PrivateCloudDriveDbContext>()
            .Database
            .MigrateAsync();
    }
}
