using Microsoft.Extensions.DependencyInjection;

namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示AppServices组件，封装对应业务场景的状态或行为。
/// </summary>
public static class AppServices
{
    public static IServiceProvider Current { get; private set; } = default!;

    /// <summary>
    /// 执行Initialize操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        Current = serviceProvider;
    }

    /// <summary>
    /// 从当前 MAUI 服务容器解析必需服务，保证页面和服务层使用统一依赖实例。
    /// </summary>
    public static T GetRequiredService<T>()
        where T : notnull
    {
        return Current.GetRequiredService<T>();
    }
}
