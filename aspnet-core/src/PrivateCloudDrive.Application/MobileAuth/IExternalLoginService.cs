using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 供 OpenIddict 自定义 grant 调用的第三方登录领域应用服务。
/// </summary>
public interface IExternalLoginService
{
    /// <summary>
    /// 使用 Provider 授权码完成登录；未绑定时返回绑定票据而不是直接签发本地令牌。
    /// </summary>
    Task<ExternalLoginResult> LoginAsync(ExternalLoginInput input);
}
