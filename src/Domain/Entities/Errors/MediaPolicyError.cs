using System.Net;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Entities.Errors;

public static class MediaPolicyError
{
    public static readonly Error Public = new(
        ErrorCode.Lifecycle(
            nameof(MediaPolicy),
            nameof(MediaPolicy.AccessLevel),
            nameof(AccessLevel.Public)),
        HttpStatusCode.Forbidden);

    public static readonly Error Private = new(
        ErrorCode.Lifecycle(
            nameof(MediaPolicy),
            nameof(MediaPolicy.AccessLevel),
            nameof(AccessLevel.Private)),
        HttpStatusCode.Forbidden);

    public static readonly Error TooLarge = new(
        ErrorCode.Evaluate(
            nameof(MediaPolicy),
            nameof(MediaPolicy.MaxFileSizeBytes),
            EvaluateViolation.TooLarge),
        HttpStatusCode.UnprocessableEntity);

    public static readonly Error TooMany = new(
        ErrorCode.Evaluate(
            nameof(MediaPolicy),
            nameof(MediaPolicy.MaxMediaCount),
            EvaluateViolation.TooMany),
        HttpStatusCode.UnprocessableEntity);

    public static readonly Error UnsupportedContentType = new(
        ErrorCode.Evaluate(
            nameof(MediaPolicy),
            nameof(MediaPolicy.AllowedContentTypes),
            EvaluateViolation.Invalid),
        HttpStatusCode.UnprocessableEntity);
}
