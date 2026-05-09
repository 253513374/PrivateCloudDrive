using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace PrivateCloudDrive;

/// <summary>
/// 表示PrivateCloudDriveDtoExtensions组件，封装对应业务场景的状态或行为。
/// </summary>
public static class PrivateCloudDriveDtoExtensions
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
                /* You can add extension properties to DTOs
                 * defined in the depended modules.
                 *
                 * Example:
                 *
                 * ObjectExtensionManager.Instance
                 *   .AddOrUpdateProperty<IdentityRoleDto, string>("Title");
                 *
                 * See the documentation for more:
                 * https://docs.abp.io/en/abp/latest/Object-Extensions
                 */
        });
    }
}
