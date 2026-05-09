using System.Threading.Tasks;
using PrivateCloudDrive.Localization;
using Volo.Abp.UI.Navigation;

namespace PrivateCloudDrive.Menus;

/// <summary>
/// 表示PrivateCloudDriveMenuContributor组件，封装对应业务场景的状态或行为。
/// </summary>
public class PrivateCloudDriveMenuContributor : IMenuContributor
{
    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            var l = context.GetLocalizer<PrivateCloudDriveResource>();

            context.Menu.AddItem(
                new ApplicationMenuItem(
                    PrivateCloudDriveMenus.FileCenter,
                    l["Menu:FileCenter"],
                    "/swagger",
                    "fa fa-folder",
                    20));
        }

        return Task.CompletedTask;
    }
}
