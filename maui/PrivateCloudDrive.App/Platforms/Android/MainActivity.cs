using Android.App;
using Android.Content.PM;
using Android.OS;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示MainActivity组件，封装对应业务场景的状态或行为。
/// </summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
