using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示移动认证WebAuthenticationCallbackActivity，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "privateclouddrive",
    DataHost = "callback")]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
