using System;
using System.Collections.Generic;
using System.Text;
using PrivateCloudDrive.Localization;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive;

/* Inherit your application services from this class.
 */
public abstract class PrivateCloudDriveAppService : ApplicationService
{
    protected PrivateCloudDriveAppService()
    {
        LocalizationResource = typeof(PrivateCloudDriveResource);
    }
}
