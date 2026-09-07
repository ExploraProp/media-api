using Media.Domain.Entities.Definitions;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Application.Services.Interfaces;

public interface IMediaEventAccessService
{
    Task<Result<bool>> CanAccessEventsAsync(
        MediaFile mediaFile,
        AuthId authId,
        UploadedByClientId? clientUserId,
        CancellationToken ct);
}
