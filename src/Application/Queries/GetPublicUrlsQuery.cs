using Media.Application.Dto;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.Queries;

public sealed record GetPublicUrlsQuery(PublicUrlsRequest Request)
    : IRequest<Result<PublicUrlsResponse>>
{
    public static Result<GetPublicUrlsQuery> FromDto(PublicUrlsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Result<GetPublicUrlsQuery>.Success(new GetPublicUrlsQuery(request));
    }
}
