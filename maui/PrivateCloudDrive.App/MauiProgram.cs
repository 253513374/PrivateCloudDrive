using Microsoft.Extensions.Logging;

using CommunityToolkit.Maui;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示MauiProgram组件，封装对应业务场景的状态或行为。
/// </summary>
public static class MauiProgram
{
	/// <summary>
	/// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
	/// </summary>
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<IAuthService, OpenIddictAuthService>();
		builder.Services.AddSingleton<ICloudDriveApiClient, CloudDriveApiClient>();
		builder.Services.AddSingleton<IUploadQueueService, UploadQueueService>();
#if ANDROID
		builder.Services.AddSingleton<IWechatPlatformAuthService, AndroidWechatPlatformAuthService>();
#else
		builder.Services.AddSingleton<IWechatPlatformAuthService, DefaultWechatPlatformAuthService>();
#endif

		var app = builder.Build();
		AppServices.Initialize(app.Services);

		return app;
	}
}
