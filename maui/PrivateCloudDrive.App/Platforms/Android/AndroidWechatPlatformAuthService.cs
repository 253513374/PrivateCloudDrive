using System.Security.Cryptography;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Runtime;
using Microsoft.Maui.ApplicationModel;
using PrivateCloudDrive.App.Models;

namespace PrivateCloudDrive.App.Services;

public sealed class AndroidWechatPlatformAuthService : IWechatPlatformAuthService
{
    private const string DefaultWechatScope = "snsapi_userinfo";
    private const string BridgeClassName = "com/companyname/privateclouddrive/app/wechat/WechatAuthBridge";
    private const string AuthResultAction = "com.companyname.privateclouddrive.app.WECHAT_AUTH_RESULT";
    private static readonly TimeSpan AuthorizationTimeout = TimeSpan.FromMinutes(5);
    private WechatAuthResultReceiver? _receiver;

    public Task<bool> IsAvailableAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!CanUseSettings(settings, out var appId))
        {
            return Task.FromResult(false);
        }

        var context = Platform.AppContext;
        return Task.FromResult(WechatAuthBridge.IsWechatInstalled(context, appId));
    }

    public async Task<WechatPlatformAuthResult> AuthorizeAsync(
        WechatLoginSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!CanUseSettings(settings, out var appId))
        {
            return WechatPlatformAuthResult.Failure("WeChat login is not configured.");
        }

        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            return WechatPlatformAuthResult.Failure("Android activity is not available.");
        }

        if (!WechatAuthBridge.IsWechatInstalled(activity, appId))
        {
            return WechatPlatformAuthResult.Failure("WeChat is not installed on this device.");
        }

        RegisterReceiver(activity);

        var state = CreateState();
        var resultTask = WechatAuthCallbackStore.BeginAsync(state, AuthorizationTimeout, cancellationToken);

        var scope = string.IsNullOrWhiteSpace(settings.Scope)
            ? DefaultWechatScope
            : settings.Scope.Trim();

        if (!WechatAuthBridge.SendAuth(activity, appId, scope, state))
        {
            WechatAuthCallbackStore.Fail("Unable to start WeChat authorization.");
        }

        return await resultTask;
    }

    private void RegisterReceiver(Context context)
    {
        if (_receiver != null)
        {
            return;
        }

        _receiver = new WechatAuthResultReceiver();
        var filter = new IntentFilter(AuthResultAction);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            context.RegisterReceiver(_receiver, filter, ReceiverFlags.NotExported);
            return;
        }

        context.RegisterReceiver(_receiver, filter);
    }

    private static bool CanUseSettings(WechatLoginSettings settings, out string appId)
    {
        appId = settings.AppId?.Trim() ?? string.Empty;
        return settings.IsEnabled &&
               !string.IsNullOrWhiteSpace(appId);
    }

    private static string CreateState()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class WechatAuthResultReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null)
            {
                WechatAuthCallbackStore.Fail("WeChat authorization callback is empty.");
                return;
            }

            var code = intent.GetStringExtra("code");
            var state = intent.GetStringExtra("state");
            var error = intent.GetStringExtra("error");

            WechatAuthCallbackStore.Complete(code, state, error);
        }
    }

    private static class WechatAuthBridge
    {
        public static bool IsWechatInstalled(Context context, string appId)
        {
            using var appIdString = new Java.Lang.String(appId);
            return CallStaticBoolean(
                "isWechatInstalled",
                "(Landroid/content/Context;Ljava/lang/String;)Z",
                new JValue(context),
                new JValue(appIdString));
        }

        public static bool SendAuth(Context context, string appId, string scope, string state)
        {
            using var appIdString = new Java.Lang.String(appId);
            using var scopeString = new Java.Lang.String(scope);
            using var stateString = new Java.Lang.String(state);

            return CallStaticBoolean(
                "sendAuth",
                "(Landroid/content/Context;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)Z",
                new JValue(context),
                new JValue(appIdString),
                new JValue(scopeString),
                new JValue(stateString));
        }

        private static bool CallStaticBoolean(string methodName, string signature, params JValue[] args)
        {
            var classReference = JNIEnv.FindClass(BridgeClassName);
            try
            {
                var methodId = JNIEnv.GetStaticMethodID(classReference, methodName, signature);
                return JNIEnv.CallStaticBooleanMethod(classReference, methodId, args);
            }
            finally
            {
                JNIEnv.DeleteLocalRef(classReference);
            }
        }
    }
}
