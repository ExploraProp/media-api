using Media.Domain.ValueObjects.Ids;

namespace Media.Domain.Services.Interfaces;

public sealed record MediaFileEvent(string Type, object Payload);

public interface IMediaEventChannel
{
    void Publish(MediaFileId id, MediaFileEvent ev);
    Task<MediaFileEvent> WaitAsync(MediaFileId id, CancellationToken ct);
}
