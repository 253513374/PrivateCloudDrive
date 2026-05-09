using System.Collections.Generic;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 第三方登录公开配置集合。
/// </summary>
public class ExternalLoginSettingsDto
{
    public List<ExternalLoginProviderSettingsDto> Providers { get; set; } = [];
}
