namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 表示WechatLogin配置选项，用于集中管理运行时可调整参数。
/// </summary>
public class WechatLoginOptions
{
    public bool Enabled { get; set; }

    public string AppId { get; set; } = string.Empty;

    public string AppSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = "snsapi_userinfo";

    public string CallbackScheme { get; set; } = "privateclouddrive";

    public WechatAndroidOptions Android { get; set; } = new();

    public WechatIosOptions iOS { get; set; } = new();

    public int BindingTicketLifetimeMinutes { get; set; } = 5;

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int RateLimitWindowSeconds { get; set; } = 300;

    public int RateLimitMaxAttempts { get; set; } = 60;

    /// <summary>
    /// 执行IsUsable操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public bool IsUsable()
    {
        return Enabled &&
               !string.IsNullOrWhiteSpace(AppId) &&
               !string.IsNullOrWhiteSpace(AppSecret);
    }
}

/// <summary>
/// 表示WechatAndroid配置选项，用于集中管理运行时可调整参数。
/// </summary>
public class WechatAndroidOptions
{
    public string PackageName { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// 表示WechatIos配置选项，用于集中管理运行时可调整参数。
/// </summary>
public class WechatIosOptions
{
    public string BundleId { get; set; } = string.Empty;

    public string UrlScheme { get; set; } = string.Empty;
}
