using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 提供IWechatAuth相关应用服务编排，承接权限校验、业务规则调用与 DTO 映射。
/// </summary>
public interface IWechatAuthAppService : IApplicationService
{
    Task<WechatLoginSettingsDto> GetSettingsAsync();

    Task<WechatBindingDto?> GetBindingAsync();

    Task<WechatBindingDto> BindCurrentAsync(BindCurrentWechatInput input);

    Task<WechatBindingDto> BindExistingAsync(BindExistingWechatInput input);

    Task UnbindAsync();
}
