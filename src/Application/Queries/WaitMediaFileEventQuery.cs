using Media.Application.Dto;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Application.Queries;

public sealed record WaitMediaFileEventQuery(
    MediaFileId MediaFileId,
    AuthId RequestingUserId,
    UploadedByClientId? RequestingClientUserId) : IRequest<Result<MediaFileEventWaitDto>>
{
    public static Result<WaitMediaFileEventQuery> FromId(
        string id,
        AuthId requestingUserId,
        UploadedByClientId? requestingClientUserId)
    {
        var idResult = MediaFileId.Create(id);
        return idResult.IsSuccess
            ? Result<WaitMediaFileEventQuery>.Success(new WaitMediaFileEventQuery(idResult.Value!, requestingUserId, requestingClientUserId))
            : Result<WaitMediaFileEventQuery>.FromResult(idResult);
    }
}
