using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace PrivateCloudDrive.HttpApi.Client.ConsoleTestApp;

/// <summary>
/// 提供ConsoleTestAppHosted服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public class ConsoleTestAppHostedService : IHostedService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="ConsoleTestAppHostedService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public ConsoleTestAppHostedService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 启动宿主服务或页面流程，并完成必要的初始化动作。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var application = await AbpApplicationFactory.CreateAsync<PrivateCloudDriveConsoleApiClientModule>(options =>
        {
           options.Services.ReplaceConfiguration(_configuration);
           options.UseAutofac();
        }))
        {
            await application.InitializeAsync();

            var demo = application.ServiceProvider.GetRequiredService<ClientDemoService>();
            await demo.RunAsync();

            await application.ShutdownAsync();
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
