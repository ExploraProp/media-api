using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Domain;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Domain.Entities.Definitions;

/// <summary>
/// Ephemeral quarantine upload. Terminal Rejected/Failed rows are retained for wait/GET;
/// Approved uploads are promoted to <see cref="MediaFile"/> and this row is removed.
/// </summary>
public class MediaUpload : AggregateRoot<MediaFileId>
{
    public OwnerId OwnerId { get; private set; } = null!;
    public OwnerType OwnerType { get; private set; }
    public MediaStatus Status { get; private set; }
    public StoragePath QuarantinePath { get; private set; } = null!;
    public AllowedContentTypes ContentType { get; private set; }
    public MediaFileSize FileSizeBytes { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }
    public ScanAttemptCount ScanAttempts { get; private set; } = null!;
    public UploadedByClientId UploadedByClientUserId { get; private set; } = null!;

    private MediaUpload(MediaFileId id) : base(id) { }

    [ExcludeFromCodeCoverage]
    private MediaUpload() : base(new MediaFileId(Guid.Empty)) { }

    public static Result<MediaUpload> Create(
        MediaFileId id,
        OwnerId ownerId,
        OwnerType ownerType,
        StoragePath quarantinePath,
        AllowedContentTypes contentType,
        MediaFileSize fileSizeBytes,
        UploadedByClientId uploadedByClientUserId)
    {
        var scanAttemptsResult = ScanAttemptCount.Create(0);
        if (scanAttemptsResult.IsFailure)
            return Result<MediaUpload>.FromResult(scanAttemptsResult);

        var upload = new MediaUpload(id)
        {
            OwnerId = ownerId,
            OwnerType = ownerType,
            Status = MediaStatus.Pending,
            QuarantinePath = quarantinePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = DateTime.UtcNow,
            ScanAttempts = scanAttemptsResult.Value!,
            UploadedByClientUserId = uploadedByClientUserId,
        };

        return Result<MediaUpload>.Success(upload);
    }

    public bool CanBeDeleted() => Status != MediaStatus.Scanning;

    public Result<MediaUpload> Delete()
    {
        if (Status == MediaStatus.Scanning)
            return Result<MediaUpload>.Failure(MediaUploadError.OngoingScanning);

        RaiseDomainEvent(new FileDeleted(Id, OwnerId, OwnerType));
        return Result<MediaUpload>.Success(this);
    }

    public Result<MediaUpload> Approve()
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaUpload>.Failure(MediaUploadError.NotScanning);

        Status = MediaStatus.Approved;
        UpdateTimestamps();
        return Result<MediaUpload>.Success(this);
    }

    public Result<MediaUpload> Reject(string reason)
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaUpload>.Failure(MediaUploadError.NotScanning);

        Status = MediaStatus.Rejected;
        RaiseDomainEvent(new FileRejected(OwnerId, OwnerType, reason, Id));
        UpdateTimestamps();
        return Result<MediaUpload>.Success(this);
    }

    public Result<MediaUpload> MarkFailed(string reason)
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaUpload>.Failure(MediaUploadError.NotScanning);

        Status = MediaStatus.Failed;
        RaiseDomainEvent(new FileRejected(OwnerId, OwnerType, reason, Id));
        UpdateTimestamps();
        return Result<MediaUpload>.Success(this);
    }

    public Result<MediaUpload> IncrementScanAttempt()
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaUpload>.Failure(MediaUploadError.NotScanning);

        ScanAttempts = ScanAttemptCount.FromDatabase(ScanAttempts.Value + 1);
        UpdateTimestamps();
        return Result<MediaUpload>.Success(this);
    }

    public Result<MediaUpload> BeginScan()
    {
        if (Status != MediaStatus.Pending)
            return Result<MediaUpload>.Failure(MediaUploadError.NotPending);

        Status = MediaStatus.Scanning;
        ScanAttempts = ScanAttemptCount.FromDatabase(ScanAttempts.Value + 1);
        UpdateTimestamps();
        return Result<MediaUpload>.Success(this);
    }

    [ExcludeFromCodeCoverage]
    protected override void When(DomainEvent domainEvent)
    {
    }
}
