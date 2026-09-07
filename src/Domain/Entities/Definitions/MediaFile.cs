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

public class MediaFile : AggregateRoot<MediaFileId>
{
    private List<MediaVariant> _variants = [];

    public OwnerId OwnerId { get; private set; } = null!;
    public OwnerType OwnerType { get; private set; }
    public MediaType MediaType { get; private set; }
    public MediaStatus Status { get; private set; }
    public StoragePath QuarantinePath { get; private set; } = null!;
    public Link? PublicUrl { get; private set; }
    public AccessLevel AccessLevel { get; private set; }
    public AllowedContentTypes ContentType { get; private set; }
    public MediaFileSize FileSizeBytes { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public ScanAttemptCount ScanAttempts { get; private set; } = null!;
    public UploadedByClientId UploadedByClientUserId { get; private set; } = null!;
    public IReadOnlyList<MediaVariant> Variants => _variants.AsReadOnly();
    public bool IsApproved => Status == MediaStatus.Approved;
    public bool IsPrivate => AccessLevel == AccessLevel.Private;

    private MediaFile(MediaFileId id) : base(id) { }

    [ExcludeFromCodeCoverage]
    private MediaFile() : base(new MediaFileId(Guid.Empty)) { }

    public static Result<MediaFile> Create(
        MediaFileId id,
        OwnerId ownerId,
        OwnerType ownerType,
        MediaType mediaType,
        MediaStatus status,
        StoragePath quarantinePath,
        AllowedContentTypes contentType,
        MediaFileSize fileSizeBytes,
        UploadedByClientId uploadedByClientUserId)
    {
        var mediaFile = new MediaFile(id)
        {
            OwnerId = ownerId,
            OwnerType = ownerType,
            MediaType = mediaType,
            Status = status,
            QuarantinePath = quarantinePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = DateTime.UtcNow,
            ScanAttempts = ScanAttemptCount.Zero(),
            UploadedByClientUserId = uploadedByClientUserId,
            AccessLevel = AccessLevel.Private,
        };

        return Result<MediaFile>.Success(mediaFile);
    }

    public bool CanBeDeleted() => Status != MediaStatus.Scanning;

    public Result<MediaFile> Delete()
    {
        if (Status == MediaStatus.Scanning)
            return Result<MediaFile>.Failure(MediaFileError.OngoingScanning);

        RaiseDomainEvent(new FileDeleted(Id, OwnerId, OwnerType));
        return Result<MediaFile>.Success(this);
    }

    public Result<MediaFile> Approve(
        string? publicUrl,
        AccessLevel accessLevel,
        IReadOnlyList<PromotedVariantInput> promotedVariants)
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaFile>.Failure(MediaFileError.NotScanning);

        Status = MediaStatus.Approved;
        AccessLevel = accessLevel;
        PublicUrl = publicUrl != null ? Link.FromDatabase(publicUrl) : null;
        ApprovedAt = DateTime.UtcNow;

        _variants.AddRange(
            promotedVariants
                .Select(input => MediaVariant.Create(
                    Id,
                    input.VariantType,
                    input.Url,
                    input.Width,
                    input.Height,
                    input.FileSizeBytes))
        );


        var eventVariants = _variants
            .Select(v => new MediaVariantSnapshot(v.VariantType.ToI18nCode(), v.Url.Value))
            .ToList();

        RaiseDomainEvent(new FileApproved(
            publicUrl,
            OwnerId,
            OwnerType,
            accessLevel,
            eventVariants,
            Id));

        UpdateTimestamps();
        return Result<MediaFile>.Success(this);
    }

    public Result<MediaFile> Reject(string reason)
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaFile>.Failure(MediaFileError.NotScanning);

        Status = MediaStatus.Rejected;
        RaiseDomainEvent(new FileRejected(OwnerId, OwnerType, reason, Id));
        UpdateTimestamps();
        return Result<MediaFile>.Success(this);
    }

    public Result<MediaFile> MarkFailed(string reason)
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaFile>.Failure(MediaFileError.NotScanning);

        Status = MediaStatus.Failed;
        RaiseDomainEvent(new FileRejected(OwnerId, OwnerType, reason, Id));
        UpdateTimestamps();
        return Result<MediaFile>.Success(this);
    }

    public Result<MediaFile> IncrementScanAttempt()
    {
        if (Status != MediaStatus.Scanning)
            return Result<MediaFile>.Failure(MediaFileError.NotScanning);

        ScanAttempts = ScanAttemptCount.FromDatabase(ScanAttempts.Value + 1);
        UpdateTimestamps();
        return Result<MediaFile>.Success(this);
    }

    public Result<MediaFile> BeginScan()
    {
        if (Status != MediaStatus.Pending)
            return Result<MediaFile>.Failure(MediaFileError.NotPending);

        Status = MediaStatus.Scanning;
        ScanAttempts = ScanAttemptCount.FromDatabase(ScanAttempts.Value + 1);
        UpdateTimestamps();
        return Result<MediaFile>.Success(this);
    }

    [ExcludeFromCodeCoverage]
    protected override void When(DomainEvent domainEvent)
    {
    }
}
