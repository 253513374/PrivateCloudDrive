using System;
using System.Threading.Tasks;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 保存首次第三方登录产生的短期绑定票据，用于后续绑定已有账号。
/// </summary>
public interface IExternalBindingTicketStore
{
    /// <summary>
    /// 为未绑定的第三方身份创建一次性短期票据。
    /// </summary>
    Task<string> CreateAsync(ExternalIdentity identity, TimeSpan lifetime);

    /// <summary>
    /// 根据票据读取第三方身份；票据不存在或过期时返回 null。
    /// </summary>
    Task<ExternalIdentity?> GetAsync(string ticket);

    /// <summary>
    /// 绑定完成后删除票据，避免重复使用。
    /// </summary>
    Task RemoveAsync(string ticket);
}
