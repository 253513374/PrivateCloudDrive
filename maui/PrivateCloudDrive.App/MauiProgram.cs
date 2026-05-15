using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

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
		ConfigureNativeControlStyling();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("JetBrainsMono-wght.ttf", "JetBrainsMono");
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

	private static void ConfigureNativeControlStyling()
	{
#if ANDROID
		EntryHandler.Mapper.AppendToMapping("NoDefaultUnderline", (handler, _) =>
		{
			ClearAndroidTextInputBackground(handler.PlatformView);
		});

		EditorHandler.Mapper.AppendToMapping("NoDefaultUnderline", (handler, _) =>
		{
			ClearAndroidTextInputBackground(handler.PlatformView);
		});
#endif
	}

#if ANDROID
	private static void ClearAndroidTextInputBackground(Android.Widget.TextView textView)
	{
		ApplyAndroidTextInputBackgroundFix(textView);
		textView.Post(() => ApplyAndroidTextInputBackgroundFix(textView));
	}

	private static void ApplyAndroidTextInputBackgroundFix(Android.Widget.TextView textView)
	{
		textView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
		textView.BackgroundTintMode = Android.Graphics.PorterDuff.Mode.Clear;
		textView.Background = null;
		textView.SetBackgroundColor(Android.Graphics.Color.Transparent);
		textView.SetPadding(textView.PaddingLeft, 0, textView.PaddingRight, 0);
		textView.Invalidate();
	}
#endif
}
