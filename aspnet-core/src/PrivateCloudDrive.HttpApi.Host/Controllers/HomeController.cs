using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace PrivateCloudDrive.Controllers;

/// <summary>
/// 提供Home相关 HTTP API 入口，负责请求绑定并委托应用服务处理业务逻辑。
/// </summary>
public class HomeController : AbpController
{
    /// <summary>
    /// 执行Index操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public ActionResult Index()
    {
        return Redirect("~/swagger");
    }
}
