using Media.Application.Dto;
using Media.Application.Queries;
using Media.Application.Services.Interfaces;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class GetPublicUrlsUseCase(IMediaPublicUrlService mediaPublicUrlService)
    : IRequestHandler<GetPublicUrlsQuery, Result<PublicUrlsResponse>>
{
    public async Task<Result<PublicUrlsResponse>> Handle(
        GetPublicUrlsQuery request,
        CancellationToken cancellationToken)
    {
        var urls = await mediaPublicUrlService.GetPublicUrlsAsync(
            request.Request.MediaFileIds,
            cancellationToken);
        return Result<PublicUrlsResponse>.Success(new PublicUrlsResponse(urls));
    }
}
