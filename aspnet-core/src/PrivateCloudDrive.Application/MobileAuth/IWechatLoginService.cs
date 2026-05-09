using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 提供IWechatLogin服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public interface IWechatLoginService
{
    Task<WechatLoginResult> LoginAsync(WechatLoginInput input);
}
