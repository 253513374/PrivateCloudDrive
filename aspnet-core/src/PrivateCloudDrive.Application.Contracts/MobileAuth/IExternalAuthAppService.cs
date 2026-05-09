using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 面向 MAUI 客户端的第三方登录账号绑定应用服务。
/// 只暴露公开配置、当前用户绑定状态、绑定和解绑能力，不返回任何 Provider Secret 或 Token。
/// </summary>
public interface IExternalAuthAppService : IApplicationService
{
    /// <summary>
    /// 获取 Google、GitHub 等第三方登录 Provider 的公开配置，用于客户端决定是否显示登录入口。
    /// </summary>
    Task<ExternalLoginSettingsDto> GetSettingsAsync();

    /// <summary>
    /// 获取当前登录用户已经绑定且仍启用的第三方账号列表。
    /// </summary>
    Task<IReadOnlyList<ExternalBindingDto>> GetBindingsAsync();

    /// <summary>
    /// 当前已登录用户通过授权码直接绑定第三方账号。
    /// </summary>
    Task<ExternalBindingDto> BindCurrentAsync(BindCurrentExternalLoginInput input);

    /// <summary>
    /// 首次第三方登录返回绑定票据后，使用账号密码把第三方身份绑定到已有用户。
    /// </summary>
    Task<ExternalBindingDto> BindExistingAsync(BindExistingExternalLoginInput input);

    /// <summary>
    /// 解绑当前用户指定 Provider 的第三方账号；必须保留账号密码登录能力，避免用户失去登录入口。
    /// </summary>
    Task UnbindAsync(string provider);
}
