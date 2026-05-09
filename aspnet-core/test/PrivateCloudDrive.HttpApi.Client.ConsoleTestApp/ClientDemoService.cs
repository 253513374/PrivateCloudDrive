using System;
using System.Threading.Tasks;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.HttpApi.Client.ConsoleTestApp;

/// <summary>
/// 提供ClientDemo服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public class ClientDemoService : ITransientDependency
{
    private readonly IProfileAppService _profileAppService;

    /// <summary>
    /// 初始化 <see cref="ClientDemoService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public ClientDemoService(IProfileAppService profileAppService)
    {
        _profileAppService = profileAppService;
    }

    /// <summary>
    /// 执行Run操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public async Task RunAsync()
    {
        var output = await _profileAppService.GetAsync();
        Console.WriteLine($"UserName : {output.UserName}");
        Console.WriteLine($"Email    : {output.Email}");
        Console.WriteLine($"Name     : {output.Name}");
        Console.WriteLine($"Surname  : {output.Surname}");
    }
}
