using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PrivateCloudDrive.HttpApi.Client.ConsoleTestApp;

class Program
{
    static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).RunConsoleAsync();
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .AddAppSettingsSecretsJson()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ConsoleTestAppHostedService>();
            });
}
