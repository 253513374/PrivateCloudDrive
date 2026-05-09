using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.MobileAuth;

namespace PrivateCloudDrive.Controllers.MobileAuth;

/// <summary>
/// MAUI 客户端第三方账号绑定管理 API。
/// </summary>
[Route("api/mobile-auth/external")]
public class MobileAuthExternalController : PrivateCloudDriveController
{
    private readonly IExternalAuthAppService _externalAuthAppService;

    /// <summary>
    /// 初始化 <see cref="MobileAuthExternalController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public MobileAuthExternalController(IExternalAuthAppService externalAuthAppService)
    {
        _externalAuthAppService = externalAuthAppService;
    }

    /// <summary>
    /// 获取第三方登录公开配置。
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    public virtual Task<ExternalLoginSettingsDto> GetSettingsAsync()
    {
        return _externalAuthAppService.GetSettingsAsync();
    }

    /// <summary>
    /// 获取当前用户第三方账号绑定列表。
    /// </summary>
    [HttpGet("bindings")]
    [Authorize]
    public virtual Task<IReadOnlyList<ExternalBindingDto>> GetBindingsAsync()
    {
        return _externalAuthAppService.GetBindingsAsync();
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    [HttpPost("bind-current")]
    [Authorize]
    public virtual Task<ExternalBindingDto> BindCurrentAsync([FromBody] BindCurrentExternalLoginInput input)
    {
        return _externalAuthAppService.BindCurrentAsync(input);
    }

    /// <summary>
    /// 绑定第三方身份与当前或指定账号，并防止同一外部身份被重复占用。
    /// </summary>
    [HttpPost("bind-existing")]
    [AllowAnonymous]
    public virtual Task<ExternalBindingDto> BindExistingAsync([FromBody] BindExistingExternalLoginInput input)
    {
        return _externalAuthAppService.BindExistingAsync(input);
    }

    /// <summary>
    /// 解除第三方身份绑定，并确保账号仍保留可用登录方式。
    /// </summary>
    [HttpDelete("bindings/{provider}")]
    [Authorize]
    public virtual Task UnbindAsync([FromRoute] string provider)
    {
        return _externalAuthAppService.UnbindAsync(provider);
    }
}
