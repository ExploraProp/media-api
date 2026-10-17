using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Application.Services.Interfaces;

public interface IMediaEventAccessService
{
    Task<Result<bool>> CanAccessEventsAsync(
        OwnerId ownerId,
        OwnerType ownerType,
        AuthId authId,
        UploadedByClientId? clientUserId,
        CancellationToken ct);
}
