using PrivateCloudDrive.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace PrivateCloudDrive.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class PrivateCloudDriveController : AbpControllerBase
{
    protected PrivateCloudDriveController()
    {
        LocalizationResource = typeof(PrivateCloudDriveResource);
    }
}
