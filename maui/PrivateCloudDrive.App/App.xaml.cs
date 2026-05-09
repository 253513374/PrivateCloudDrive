using Microsoft.Extensions.DependencyInjection;

using PrivateCloudDrive.App.Localization;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示App组件，封装对应业务场景的状态或行为。
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// 初始化 <see cref="App"/> 的新实例，并注入完成业务处理所需的依赖。
	/// </summary>
	public App()
	{
		AppText.UseDefaultCulture();
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
