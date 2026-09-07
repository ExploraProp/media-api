using System.Net;
using Media.Domain.Services;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Services.Errors;

public static class MarketingServiceError
{
    public static readonly Error DownstreamFailure = new(
        ErrorCode.Evaluate(
            nameof(MarketingService),
            nameof(MarketingService.Response),
            EvaluateViolation.Invalid),
        HttpStatusCode.ServiceUnavailable);
}
