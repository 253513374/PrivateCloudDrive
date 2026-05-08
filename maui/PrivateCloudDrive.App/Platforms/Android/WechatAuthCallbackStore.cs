namespace PrivateCloudDrive.App.Services;

public static class WechatAuthCallbackStore
{
    private static readonly object SyncRoot = new();
    private static PendingWechatAuth? _pending;

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

        public PendingWechatAuth(string state)
        {
            State = state;
        }

        public string State { get; }

        public Task<WechatPlatformAuthResult> Task => _completion.Task;

        public void TrySetResult(WechatPlatformAuthResult result)
        {
            _completion.TrySetResult(result);
        }
    }
}
