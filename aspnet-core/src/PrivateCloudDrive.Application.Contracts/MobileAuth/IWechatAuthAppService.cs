using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.MobileAuth;

public interface IWechatAuthAppService : IApplicationService
{
    Task<WechatLoginSettingsDto> GetSettingsAsync();

    Task<WechatBindingDto?> GetBindingAsync();

    Task<WechatBindingDto> BindCurrentAsync(BindCurrentWechatInput input);

    Task<WechatBindingDto> BindExistingAsync(BindExistingWechatInput input);

    Task UnbindAsync();
}
