using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Volo.Abp.Autofac;
using Volo.Abp.Http.Client;
using Volo.Abp.Http.Client.IdentityModel;
using Volo.Abp.Modularity;

namespace PrivateCloudDrive.HttpApi.Client.ConsoleTestApp;

/// <summary>
/// 配置PrivateCloudDriveConsoleApiClientModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PrivateCloudDriveHttpApiClientModule),
    typeof(AbpHttpClientIdentityModelModule)
    )]
public class PrivateCloudDriveConsoleApiClientModule : AbpModule
{
    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<AbpHttpClientBuilderOptions>(options =>
        {
            options.ProxyClientBuildActions.Add((remoteServiceName, clientBuilder) =>
            {
                clientBuilder.AddTransientHttpErrorPolicy(
                    policyBuilder => policyBuilder.WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)))
                );
            });
        });
    }
}
