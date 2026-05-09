using PrivateCloudDrive.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace PrivateCloudDrive.DbMigrator;

/// <summary>
/// 配置PrivateCloudDriveDbMigratorModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PrivateCloudDriveEntityFrameworkCoreModule),
    typeof(PrivateCloudDriveApplicationContractsModule)
    )]
public class PrivateCloudDriveDbMigratorModule : AbpModule
{
}
