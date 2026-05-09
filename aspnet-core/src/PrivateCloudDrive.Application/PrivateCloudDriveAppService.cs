using System;
using System.Collections.Generic;
using System.Text;
using PrivateCloudDrive.Localization;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive;

/* Inherit your application services from this class.
 */
/// <summary>
/// 提供PrivateCloudDrive相关应用服务编排，承接权限校验、业务规则调用与 DTO 映射。
/// </summary>
public abstract class PrivateCloudDriveAppService : ApplicationService
{
    protected PrivateCloudDriveAppService()
    {
        LocalizationResource = typeof(PrivateCloudDriveResource);
    }
}
