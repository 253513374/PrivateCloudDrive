using System.Threading.Tasks;
using PrivateCloudDrive.Localization;
using Volo.Abp.UI.Navigation;

namespace PrivateCloudDrive.Menus;

public class PrivateCloudDriveMenuContributor : IMenuContributor
{
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
