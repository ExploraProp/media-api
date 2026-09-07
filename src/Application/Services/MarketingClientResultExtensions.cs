using System.Net;
using ExploraProp.Marketing.Internal.ApiClient;
using Media.Domain.Services.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Application.Services;

internal static class MarketingClientResultExtensions
{
    public static Result<bool> ToListingAccessResult(this MarketingClientResult<bool> result)
    {
        if (result.IsSuccess)
            return Result<bool>.Success(result.Value);

        return result.StatusCode switch
        {
            HttpStatusCode.NotFound => Result<bool>.Failure(SharedFailures.NotFound("Listing")),
            HttpStatusCode.Forbidden => Result<bool>.Failure(SharedFailures.Forbidden),
            _ => Result<bool>.Failure(MarketingServiceError.DownstreamFailure)
        };
    }
}
