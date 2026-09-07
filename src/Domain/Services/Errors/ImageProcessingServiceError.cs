using System.Net;
using Media.Domain.Services;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Services.Errors;

public static class ImageProcessingServiceError
{
    public static readonly Error Processing = new(
        ErrorCode.Evaluate(
            nameof(ImageProcessingService),
            nameof(ImageProcessingService.Response),
            EvaluateViolation.Invalid),
        HttpStatusCode.InternalServerError);
}
