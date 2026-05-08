using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrivateCloudDrive.EntityFrameworkCore.FileCenter;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.MobileAuth;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Uow;

namespace PrivateCloudDrive.EntityFrameworkCore;

[DependsOn(
    typeof(PrivateCloudDriveApplicationTestModule),
    typeof(PrivateCloudDriveEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
    )]
public class PrivateCloudDriveEntityFrameworkCoreTestModule : AbpModule
{
    private SqliteConnection? _sqliteConnection;

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FeatureManagementOptions>(options =>
        {
            options.SaveStaticFeaturesToDatabase = false;
            options.IsDynamicFeatureStoreEnabled = false;
        });
        Configure<PermissionManagementOptions>(options =>
        {
            options.SaveStaticPermissionsToDatabase = false;
            options.IsDynamicPermissionStoreEnabled = false;
        });
        Configure<SettingManagementOptions>(options =>
        {
            options.SaveStaticSettingsToDatabase = false;
            options.IsDynamicSettingStoreEnabled = false;
        });
        context.Services.AddAlwaysDisableUnitOfWorkTransaction();
        context.Services.Replace(
            ServiceDescriptor.Transient<IFileCenterVideoProcessor, TestFileCenterVideoProcessor>());
        context.Services.Replace(
            ServiceDescriptor.Transient<IWechatIdentityService, TestWechatIdentityService>());

        Configure<WechatLoginOptions>(options =>
        {
            options.Enabled = true;
            options.AppId = TestWechatIdentityService.AppId;
            options.AppSecret = "test-wechat-secret";
            options.CallbackScheme = "privateclouddrive";
            options.Android.PackageName = "com.companyname.privateclouddrive.app";
            options.iOS.BundleId = "com.companyname.privateclouddrive.app";
            options.iOS.UrlScheme = "privateclouddrive";
            options.BindingTicketLifetimeMinutes = 5;
            options.RateLimitWindowSeconds = 300;
            options.RateLimitMaxAttempts = 20;
        });

        ConfigureInMemorySqlite(context.Services);
    }

    private void ConfigureInMemorySqlite(IServiceCollection services)
    {
        _sqliteConnection = CreateDatabaseAndGetConnection();

        services.Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(context =>
            {
                context.DbContextOptions.UseSqlite(_sqliteConnection);
            });
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        _sqliteConnection?.Dispose();
    }

    private static SqliteConnection CreateDatabaseAndGetConnection()
    {
        var connection = new AbpUnitTestSqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PrivateCloudDriveDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new PrivateCloudDriveDbContext(options))
        {
            context.GetService<IRelationalDatabaseCreator>().CreateTables();
        }

        return connection;
    }
}
