using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Banking.BuildingBlocks.Resilience;

public sealed class IdempotencyReplayProtectionMiddleware(
    RequestDelegate next,
    IMemoryCache memoryCache,
    IOptions<RequestProtectionOptions> options)
{
    private static readonly string[] ProtectedMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = options.Value;
        if (!settings.EnableIdempotencyReplayBackoff ||
            !IsProtectedMethod(context.Request.Method) ||
            !context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues))
        {
            await next(context);
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString().Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var trackerKey = BuildTrackerKey(context, idempotencyKey);
        var tracker = memoryCache.GetOrCreate(trackerKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                Math.Max(settings.ReplayCacheLifetimeSeconds, settings.ReplayTrackingWindowSeconds));
            return new ReplayTracker();
        })!;

        int observedCount;
        lock (tracker.SyncRoot)
        {
            tracker.ObservedCount++;
            observedCount = tracker.ObservedCount;
        }

        if (observedCount > 1)
        {
            var replayAttempt = observedCount - 1;
            context.Response.Headers["X-Idempotent-Replay"] = "true";
            context.Response.Headers["X-Idempotency-Replay-Attempt"] = replayAttempt.ToString();

            var delay = CalculateReplayDelay(replayAttempt, settings);
            if (delay > TimeSpan.Zero)
            {
                context.Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds)).ToString();
                context.Response.Headers["X-Request-Backoff-Applied"] = ((int)delay.TotalMilliseconds).ToString();
                await Task.Delay(delay, context.RequestAborted);
            }
        }

        await next(context);
    }

    private static bool IsProtectedMethod(string method)
        => ProtectedMethods.Contains(method, StringComparer.OrdinalIgnoreCase);

    private static string BuildTrackerKey(HttpContext context, string idempotencyKey)
    {
        var callerIdentity =
            context.User.Identity?.Name ??
            context.Connection.RemoteIpAddress?.ToString() ??
            "anonymous";

        return string.Join(
            '|',
            "idempotency-replay",
            callerIdentity,
            context.Request.Method.ToUpperInvariant(),
            context.Request.Path.Value ?? "/",
            idempotencyKey);
    }

    private static TimeSpan CalculateReplayDelay(int replayAttempt, RequestProtectionOptions settings)
    {
        if (replayAttempt <= settings.ImmediateReplayLimit)
        {
            return TimeSpan.Zero;
        }

        var delayedAttempt = replayAttempt - settings.ImmediateReplayLimit - 1;
        var delayMilliseconds = settings.ReplayBaseDelayMilliseconds * Math.Pow(2, delayedAttempt);
        return TimeSpan.FromMilliseconds(Math.Min(settings.ReplayMaxDelayMilliseconds, delayMilliseconds));
    }

    private sealed class ReplayTracker
    {
        public object SyncRoot { get; } = new();
        public int ObservedCount { get; set; }
    }
}
