using Media.Application.Dto;
using Media.Application.Services.Interfaces;
using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;

namespace Media.Application.Services;

public sealed class MediaEventWaitService(IMediaEventChannel mediaEventChannel) : IMediaEventWaitService
{
    private static readonly TimeSpan EventWaitTimeout = TimeSpan.FromSeconds(120);

    public async Task<MediaFileEventWaitDto> WaitForTerminalEventAsync(MediaFileId id, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(EventWaitTimeout);

        try
        {
            var mediaEvent = await mediaEventChannel.WaitAsync(id, timeoutCts.Token);
            return new MediaFileEventWaitDto(mediaEvent, IsTimeout: false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new MediaFileEventWaitDto(null, IsTimeout: true);
        }
    }
}
