using Microsoft.Extensions.DependencyInjection;

using PrivateCloudDrive.App.Localization;

namespace PrivateCloudDrive.App;

public partial class App : Application
{
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
