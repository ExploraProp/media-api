using System.Net;
using Media.Domain.Services;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Entities.Errors;

public static class MediaFileError
{
    public static readonly Error Permission = new(
        ErrorCode.Evaluate(
            nameof(MediaAccess),
            nameof(MediaAccess.Owner),
            EvaluateViolation.Invalid),
        HttpStatusCode.Forbidden);
}
