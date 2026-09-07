using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Application.Commands;

public sealed record DeleteMediaCommand(
    MediaFileId MediaFileId,
    UploadedByClientId? RequestingUserId,
    bool IsAdmin) : IRequest<Result<bool>>
{
    public static Result<DeleteMediaCommand> FromId(string rawId, UploadedByClientId? requestingUserId, bool isAdmin)
    {
        var idResult = MediaFileId.Create(rawId);
        return idResult.IsSuccess
            ? Result<DeleteMediaCommand>.Success(new DeleteMediaCommand(idResult.Value!, requestingUserId, isAdmin))
            : Result<DeleteMediaCommand>.FromResult(idResult);
    }
}
