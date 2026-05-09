using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PrivateCloudDrive.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
/// <summary>
/// 表示PrivateCloudDriveDbContextFactory组件，封装对应业务场景的状态或行为。
/// </summary>
public class PrivateCloudDriveDbContextFactory : IDesignTimeDbContextFactory<PrivateCloudDriveDbContext>
{
    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public PrivateCloudDriveDbContext CreateDbContext(string[] args)
    {
        // https://www.npgsql.org/efcore/release-notes/6.0.html#opting-out-of-the-new-timestamp-mapping-logic
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        PrivateCloudDriveEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<PrivateCloudDriveDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Default"));

        return new PrivateCloudDriveDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../PrivateCloudDrive.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
