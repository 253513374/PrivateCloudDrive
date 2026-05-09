using PrivateCloudDrive.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace PrivateCloudDrive.Controllers;

/* Inherit your controllers from this class.
 */
/// <summary>
/// 提供PrivateCloudDrive相关 HTTP API 入口，负责请求绑定并委托应用服务处理业务逻辑。
/// </summary>
public abstract class PrivateCloudDriveController : AbpControllerBase
{
    protected PrivateCloudDriveController()
    {
        LocalizationResource = typeof(PrivateCloudDriveResource);
    }
}
