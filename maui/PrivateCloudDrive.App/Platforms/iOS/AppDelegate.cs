using Foundation;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示AppDelegate组件，封装对应业务场景的状态或行为。
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
