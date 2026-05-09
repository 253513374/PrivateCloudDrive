namespace PrivateCloudDrive.App.Models;

/// <summary>
/// MAUI 客户端第三方登录配置集合。
/// </summary>
public sealed record ExternalLoginSettings(IReadOnlyList<ExternalLoginProviderSettings> Providers)
{
    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    public ExternalLoginProviderSettings? GetProvider(string provider)
    {
        return Providers.FirstOrDefault(item =>
            string.Equals(item.Provider, provider, StringComparison.OrdinalIgnoreCase));
    }
}
