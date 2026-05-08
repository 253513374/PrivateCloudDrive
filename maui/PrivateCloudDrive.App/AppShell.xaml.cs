namespace PrivateCloudDrive.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("file-details", typeof(Views.FileDetailsPage));
		Routing.RegisterRoute("media-preview", typeof(Views.MediaPreviewPage));
		Routing.RegisterRoute("operation-logs", typeof(Views.OperationLogsPage));
		Routing.RegisterRoute("trash", typeof(Views.TrashPage));
	}
}
