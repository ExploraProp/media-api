using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using System.Collections.Concurrent;

namespace Media.Infrastructure.Services;

public sealed class MediaEventChannel : IMediaEventChannel
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<MediaFileEvent>> _waiters = new();

    public void Publish(MediaFileId id, MediaFileEvent ev)
    {
        if (_waiters.TryRemove(id.Value, out var tcs))
            tcs.TrySetResult(ev);
    }

    public Task<MediaFileEvent> WaitAsync(MediaFileId id, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<MediaFileEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiters[id.Value] = tcs;
        ct.Register(() =>
        {
            if (_waiters.TryRemove(id.Value, out var registered) && ReferenceEquals(registered, tcs))
                tcs.TrySetCanceled(ct);
        });
        return tcs.Task;
    }
}
