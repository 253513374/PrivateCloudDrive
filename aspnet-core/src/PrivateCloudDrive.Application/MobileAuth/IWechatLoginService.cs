using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

public interface IWechatLoginService
{
    Task<WechatLoginResult> LoginAsync(WechatLoginInput input);
}
