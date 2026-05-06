using System.Threading.Tasks;

namespace PrivateCloudDrive.Data;

public interface IPrivateCloudDriveDbSchemaMigrator
{
    Task MigrateAsync();
}
