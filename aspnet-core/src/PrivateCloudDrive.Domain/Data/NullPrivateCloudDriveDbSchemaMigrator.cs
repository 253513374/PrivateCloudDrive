using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.Data;

/* This is used if database provider does't define
 * IPrivateCloudDriveDbSchemaMigrator implementation.
 */
public class NullPrivateCloudDriveDbSchemaMigrator : IPrivateCloudDriveDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
