using ObjCRuntime;
using UIKit;

namespace PrivateCloudDrive.App;

/// <summary>
/// 表示Program组件，封装对应业务场景的状态或行为。
/// </summary>
public class Program
{
	// This is the main entry point of the application.
	static void Main(string[] args)
	{
		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
