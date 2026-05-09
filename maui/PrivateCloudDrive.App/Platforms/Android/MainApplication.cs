using Android.App;
using Android.Runtime;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示MainApplication组件，封装对应业务场景的状态或行为。
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
	/// <summary>
	/// 初始化 <see cref="MainApplication"/> 的新实例，并注入完成业务处理所需的依赖。
	/// </summary>
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
