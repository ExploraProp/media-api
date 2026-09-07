using Media.Application.Dto;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.Queries;

public sealed record GenerateSignedUrlQuery(MediaFileId Id) : IRequest<Result<SignedUrlDto>>
{
    public static Result<GenerateSignedUrlQuery> FromId(string id)
    {
        var idResult = MediaFileId.Create(id);
        return idResult.IsSuccess
            ? Result<GenerateSignedUrlQuery>.Success(new GenerateSignedUrlQuery(idResult.Value!))
            : Result<GenerateSignedUrlQuery>.FromResult(idResult);
    }
}
