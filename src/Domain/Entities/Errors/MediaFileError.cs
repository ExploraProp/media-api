using System.Net;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Services;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Entities.Errors;

public static class MediaFileError
{
    public static readonly Error NotPending = new(
        ErrorCode.Invariant(
            nameof(MediaFile),
            nameof(MediaFile.Status),
            nameof(MediaStatus.Pending),
            InvariantViolation.Required),
        HttpStatusCode.Conflict);

    public static readonly Error NotScanning = new(
        ErrorCode.Invariant(
            nameof(MediaFile),
            nameof(MediaFile.Status),
            nameof(MediaStatus.Scanning),
            InvariantViolation.Required),
        HttpStatusCode.Conflict);

    public static readonly Error NotApproved = new(
        ErrorCode.Invariant(
            nameof(MediaFile),
            nameof(MediaFile.Status),
            nameof(MediaStatus.Approved),
            InvariantViolation.Required),
        HttpStatusCode.NotFound);

    public static readonly Error OngoingScanning = new(
        ErrorCode.Lifecycle(
            nameof(MediaFile),
            nameof(MediaFile.Status),
            nameof(MediaStatus.Scanning)),
        HttpStatusCode.Conflict);

    public static readonly Error Public = new(
        ErrorCode.Lifecycle(
            nameof(MediaFile),
            nameof(MediaFile.AccessLevel),
            nameof(AccessLevel.Public)),
        HttpStatusCode.Forbidden);

    public static readonly Error Private = new(
        ErrorCode.Lifecycle(
            nameof(MediaFile),
            nameof(MediaFile.AccessLevel),
            nameof(AccessLevel.Private)),
        HttpStatusCode.Forbidden);

    public static readonly Error Permission = new(
        ErrorCode.Evaluate(
            nameof(MediaAccess),
            nameof(MediaAccess.Owner),
            EvaluateViolation.Invalid),
        HttpStatusCode.Forbidden);
}
