using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.MobileAuth;

namespace PrivateCloudDrive.Controllers.MobileAuth;

[Route("api/mobile-auth/wechat")]
public class MobileAuthWechatController : PrivateCloudDriveController
{
    private readonly IWechatAuthAppService _wechatAuthAppService;

    public MobileAuthWechatController(IWechatAuthAppService wechatAuthAppService)
    {
        _wechatAuthAppService = wechatAuthAppService;
    }

    [HttpGet("settings")]
    [AllowAnonymous]
    public virtual Task<WechatLoginSettingsDto> GetSettingsAsync()
    {
        return _wechatAuthAppService.GetSettingsAsync();
    }

    [HttpGet("binding")]
    [Authorize]
    public virtual Task<WechatBindingDto?> GetBindingAsync()
    {
        return _wechatAuthAppService.GetBindingAsync();
    }

    [HttpPost("bind-current")]
    [Authorize]
    public virtual Task<WechatBindingDto> BindCurrentAsync([FromBody] BindCurrentWechatInput input)
    {
        return _wechatAuthAppService.BindCurrentAsync(input);
    }

    [HttpPost("bind-existing")]
    [AllowAnonymous]
    public virtual Task<WechatBindingDto> BindExistingAsync([FromBody] BindExistingWechatInput input)
    {
        return _wechatAuthAppService.BindExistingAsync(input);
    }

    [HttpDelete("binding")]
    [Authorize]
    public virtual Task UnbindAsync()
    {
        return _wechatAuthAppService.UnbindAsync();
    }
}
