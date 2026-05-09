using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrivateCloudDrive.Data;
using Serilog;
using Volo.Abp;
using Volo.Abp.Data;

namespace PrivateCloudDrive.DbMigrator;

/// <summary>
/// 提供DbMigratorHosted服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="DbMigratorHostedService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public DbMigratorHostedService(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _configuration = configuration;
    }

    /// <summary>
    /// 启动宿主服务或页面流程，并完成必要的初始化动作。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var application = await AbpApplicationFactory.CreateAsync<PrivateCloudDriveDbMigratorModule>(options =>
        {
           options.Services.ReplaceConfiguration(_configuration);
           options.UseAutofac();
           options.Services.AddLogging(c => c.AddSerilog());
           options.AddDataMigrationEnvironment();
        }))
        {
            await application.InitializeAsync();

            await application
                .ServiceProvider
                .GetRequiredService<PrivateCloudDriveDbMigrationService>()
                .MigrateAsync();

            await application.ShutdownAsync();

            _hostApplicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// 停止宿主服务并释放运行期资源。
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
