using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.MobileAuth;

namespace PrivateCloudDrive.Controllers.MobileAuth;

/// <summary>
/// 提供MobileAuthWechat相关 HTTP API 入口，负责请求绑定并委托应用服务处理业务逻辑。
/// </summary>
[Route("api/mobile-auth/wechat")]
public class MobileAuthWechatController : PrivateCloudDriveController
{
    private readonly IWechatAuthAppService _wechatAuthAppService;

    /// <summary>
    /// 初始化 <see cref="MobileAuthWechatController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MobileAuthWechatController(IWechatAuthAppService wechatAuthAppService)
    {
        _wechatAuthAppService = wechatAuthAppService;
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    public virtual Task<WechatLoginSettingsDto> GetSettingsAsync()
    {
        return _wechatAuthAppService.GetSettingsAsync();
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    [HttpGet("binding")]
    [Authorize]
    public virtual Task<WechatBindingDto?> GetBindingAsync()
    {
        return _wechatAuthAppService.GetBindingAsync();
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    [HttpPost("bind-current")]
    [Authorize]
    public virtual Task<WechatBindingDto> BindCurrentAsync([FromBody] BindCurrentWechatInput input)
    {
        return _wechatAuthAppService.BindCurrentAsync(input);
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    [HttpPost("bind-existing")]
    [AllowAnonymous]
    public virtual Task<WechatBindingDto> BindExistingAsync([FromBody] BindExistingWechatInput input)
    {
        return _wechatAuthAppService.BindExistingAsync(input);
    }

    /// <summary>
    /// 解除第三方身份绑定，并确保账号仍保留可用登录方式。
    /// </summary>
    [HttpDelete("binding")]
    [Authorize]
    public virtual Task UnbindAsync()
    {
        return _wechatAuthAppService.UnbindAsync();
    }
}
