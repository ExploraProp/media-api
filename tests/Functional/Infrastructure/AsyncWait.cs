using System.Diagnostics;

namespace Media.Functional.Infrastructure;

public static class AsyncWait
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(250);

    public static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        Func<string>? because = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);

        timeout ??= DefaultTimeout;
        interval ??= DefaultInterval;

        var stopwatch = Stopwatch.StartNew();
        var first = true;

        while (stopwatch.Elapsed < timeout.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!first)
                await Task.Delay(interval.Value, cancellationToken);
            first = false;

            if (await condition())
                return;
        }

        throw new TimeoutException(
            because?.Invoke()
            ?? $"Condition was not met within {timeout.Value.TotalSeconds:0.#}s.");
    }
}
