using Media.Application.Dto;
using Media.Domain.ValueObjects.Ids;

namespace Media.Application.Services.Interfaces;

public interface IMediaEventWaitService
{
    Task<MediaFileEventWaitDto> WaitForTerminalEventAsync(MediaFileId id, CancellationToken ct);
}
