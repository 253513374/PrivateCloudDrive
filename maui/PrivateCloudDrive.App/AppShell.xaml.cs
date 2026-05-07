namespace PrivateCloudDrive.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("media-preview", typeof(Views.MediaPreviewPage));
	}
}
