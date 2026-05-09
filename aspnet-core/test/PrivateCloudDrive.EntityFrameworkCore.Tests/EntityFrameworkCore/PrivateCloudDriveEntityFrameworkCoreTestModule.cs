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

/// <summary>
/// 配置PrivateCloudDriveEntityFrameworkCoreTestModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveApplicationTestModule),
    typeof(PrivateCloudDriveEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
    )]
public class PrivateCloudDriveEntityFrameworkCoreTestModule : AbpModule
{
    private SqliteConnection? _sqliteConnection;

    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
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
        context.Services.Replace(
            ServiceDescriptor.Transient<IExternalIdentityService, TestExternalIdentityService>());

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
        Configure<ExternalLoginOptions>(options =>
        {
            options.Google.Enabled = true;
            options.Google.ClientId = "google-test-client";
            options.Google.ClientSecret = "";
            options.Google.RedirectUri = "privateclouddrive://callback";
            options.GitHub.Enabled = true;
            options.GitHub.ClientId = "github-test-client";
            options.GitHub.ClientSecret = "github-test-secret";
            options.GitHub.RedirectUri = "privateclouddrive://callback";
            options.BindingTicketLifetimeMinutes = 5;
            options.RateLimitWindowSeconds = 300;
            options.RateLimitMaxAttempts = 20;
        });
        Configure<MobileAuthLoginOptions>(options =>
        {
            options.EnablePasswordLoginRateLimit = true;
            options.MaxFailedAttempts = 3;
            options.WindowMinutes = 15;
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

    /// <summary>
    /// 响应框架生命周期或界面事件，并协调页面状态与业务操作。
    /// </summary>
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
