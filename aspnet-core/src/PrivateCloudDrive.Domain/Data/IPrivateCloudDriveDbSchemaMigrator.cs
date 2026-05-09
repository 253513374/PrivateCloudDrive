using System.Threading.Tasks;

namespace PrivateCloudDrive.Data;

/// <summary>
/// 定义PrivateCloudDriveDbSchemaMigrator抽象契约，用于解耦调用方与具体实现。
/// </summary>
public interface IPrivateCloudDriveDbSchemaMigrator
{
    Task MigrateAsync();
}
