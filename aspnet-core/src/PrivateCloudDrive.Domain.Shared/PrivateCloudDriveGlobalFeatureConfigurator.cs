using Volo.Abp.Threading;

namespace PrivateCloudDrive;

/// <summary>
/// 表示PrivateCloudDriveGlobalFeatureConfigurator组件，封装对应业务场景的状态或行为。
/// </summary>
public static class PrivateCloudDriveGlobalFeatureConfigurator
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
                /* You can configure (enable/disable) global features of the used modules here.
                 *
                 * YOU CAN SAFELY DELETE THIS CLASS AND REMOVE ITS USAGES IF YOU DON'T NEED TO IT!
                 *
                 * Please refer to the documentation to lear more about the Global Features System:
                 * https://docs.abp.io/en/abp/latest/Global-Features
                 */
        });
    }
}
