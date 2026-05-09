namespace PrivateCloudDrive.App.Services;

/// <summary>
/// 表示移动认证WechatAuthCallbackStore，参与第三方登录、账号绑定、审计或安全控制流程。
/// </summary>
public static class WechatAuthCallbackStore
{
    private static readonly object SyncRoot = new();
    private static PendingWechatAuth? _pending;

    /// <summary>
    /// 执行Begin操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static Task<WechatPlatformAuthResult> BeginAsync(
        string state,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var pending = new PendingWechatAuth(state);

        lock (SyncRoot)
        {
            _pending?.TrySetResult(WechatPlatformAuthResult.Failure("WeChat authorization was replaced by a new request."));
            _pending = pending;
        }

        _ = CompleteOnTimeoutAsync(pending, timeout);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
                pending.TrySetResult(WechatPlatformAuthResult.Failure("WeChat authorization was canceled.")));
        }

        return pending.Task;
    }

    /// <summary>
    /// 执行Complete操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static void Complete(string? code, string? state, string? errorMessage)
    {
        PendingWechatAuth? pending;
        lock (SyncRoot)
        {
            pending = _pending;
            _pending = null;
        }

        if (pending == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(state) || !string.Equals(pending.State, state, StringComparison.Ordinal))
        {
            pending.TrySetResult(WechatPlatformAuthResult.Failure("WeChat authorization state is invalid."));
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            pending.TrySetResult(WechatPlatformAuthResult.Failure(errorMessage ?? "WeChat authorization did not return a code."));
            return;
        }

        pending.TrySetResult(new WechatPlatformAuthResult(true, code, state, "android", null));
    }

    /// <summary>
    /// 执行Fail操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    public static void Fail(string message)
    {
        PendingWechatAuth? pending;
        lock (SyncRoot)
        {
            pending = _pending;
            _pending = null;
        }

        pending?.TrySetResult(WechatPlatformAuthResult.Failure(message));
    }

    private static async Task CompleteOnTimeoutAsync(PendingWechatAuth pending, TimeSpan timeout)
    {
        await Task.Delay(timeout);

        lock (SyncRoot)
        {
            if (!ReferenceEquals(_pending, pending))
            {
                return;
            }

            _pending = null;
        }

        pending.TrySetResult(WechatPlatformAuthResult.Failure("WeChat authorization timed out."));
    }

    private sealed class PendingWechatAuth
    {
        private readonly TaskCompletionSource<WechatPlatformAuthResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// 执行PendingWechatAuth操作，封装该场景下的业务规则、异常处理和结果返回。
        /// </summary>
        public PendingWechatAuth(string state)
        {
            State = state;
        }

        public string State { get; }

        public Task<WechatPlatformAuthResult> Task => _completion.Task;

        /// <summary>
        /// 执行TrySetResult操作，封装该场景下的业务规则、异常处理和结果返回。
        /// </summary>
        public void TrySetResult(WechatPlatformAuthResult result)
        {
            _completion.TrySetResult(result);
        }
    }
}
