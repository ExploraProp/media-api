using System.Net;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Entities.Errors;

public static class MediaUploadError
{
    public static readonly Error NotPending = new(
        ErrorCode.Invariant(
            nameof(MediaUpload),
            nameof(MediaUpload.Status),
            nameof(MediaStatus.Pending),
            InvariantViolation.Required),
        HttpStatusCode.Conflict);

    public static readonly Error NotScanning = new(
        ErrorCode.Invariant(
            nameof(MediaUpload),
            nameof(MediaUpload.Status),
            nameof(MediaStatus.Scanning),
            InvariantViolation.Required),
        HttpStatusCode.Conflict);

    public static readonly Error OngoingScanning = new(
        ErrorCode.Lifecycle(
            nameof(MediaUpload),
            nameof(MediaUpload.Status),
            nameof(MediaStatus.Scanning)),
        HttpStatusCode.Conflict);
}
