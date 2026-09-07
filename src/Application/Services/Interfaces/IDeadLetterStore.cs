using Media.Domain.ValueObjects.Ids;

namespace Media.Application.Services.Interfaces;

public interface IDeadLetterStore
{
    Task RecordAsync(MediaFileId id, string reason, CancellationToken ct);
}
