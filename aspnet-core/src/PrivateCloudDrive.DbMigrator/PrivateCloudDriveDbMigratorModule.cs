using PrivateCloudDrive.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace PrivateCloudDrive.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PrivateCloudDriveEntityFrameworkCoreModule),
    typeof(PrivateCloudDriveApplicationContractsModule)
    )]
public class PrivateCloudDriveDbMigratorModule : AbpModule
{
}
