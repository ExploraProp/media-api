using Media.Application.Dto;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.Queries;

public sealed record GetMediaFileQuery(MediaFileId MediaFileId) : IRequest<Result<MediaFileDto>>
{
    public static Result<GetMediaFileQuery> FromId(string id)
    {
        var idResult = MediaFileId.Create(id);
        return idResult.IsSuccess
            ? Result<GetMediaFileQuery>.Success(new GetMediaFileQuery(idResult.Value!))
            : Result<GetMediaFileQuery>.FromResult(idResult);
    }
}
