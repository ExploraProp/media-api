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
/// Durable ready media. Created only when a <see cref="MediaUpload"/> is approved (same Id).
/// <see cref="Entity{TId}.CreatedAt"/> is the approval/ready time.
/// Access is owned by <see cref="MediaPolicy"/> for <see cref="OwnerType"/>.
/// </summary>
public class MediaFile : AggregateRoot<MediaFileId>
{
    private List<MediaVariant> _variants = [];

    public OwnerId OwnerId { get; private set; } = null!;
    public OwnerType OwnerType { get; private set; }
    /// <summary>Source MIME (jpeg/png/…); derived variants may be webp.</summary>
    public AllowedContentTypes ContentType { get; private set; }
    public UploadedByClientId UploadedByClientUserId { get; private set; } = null!;
    public IReadOnlyList<MediaVariant> Variants => _variants.AsReadOnly();

    public MediaVariant? OriginalVariant =>
        _variants.FirstOrDefault(v => v.VariantType == VariantType.Original);

    /// <summary>
    /// Public Original variant URL when <paramref name="accessLevel"/> is Public; otherwise null.
    /// </summary>
    public string? ResolvePublicUrl(AccessLevel accessLevel)
    {
        if (accessLevel != AccessLevel.Public)
            return null;

        return OriginalVariant?.Url.Value;
    }

    private MediaFile(MediaFileId id) : base(id) { }

    [ExcludeFromCodeCoverage]
    private MediaFile() : base(new MediaFileId(Guid.Empty)) { }

    public static Result<MediaFile> Create(
        MediaUpload upload,
        AccessLevel accessLevel,
        IReadOnlyList<PromotedVariantInput> promotedVariants)
    {
        var mediaFile = new MediaFile(upload.Id)
        {
            OwnerId = upload.OwnerId,
            OwnerType = upload.OwnerType,
            ContentType = upload.ContentType,
            UploadedByClientUserId = upload.UploadedByClientUserId,
        };

        mediaFile._variants.AddRange(
            promotedVariants.Select(input => MediaVariant.Create(
                upload.Id,
                input.VariantType,
                input.Url,
                input.Metadata,
                input.FileSizeBytes)));

        var eventVariants = mediaFile._variants
            .Select(v => new MediaVariantSnapshot(v.VariantType.ToI18nCode(), v.Url.Value))
            .ToList();

        mediaFile.RaiseDomainEvent(new FileApproved(
            mediaFile.ResolvePublicUrl(accessLevel),
            upload.OwnerId,
            upload.OwnerType,
            accessLevel,
            eventVariants,
            upload.Id));

        return Result<MediaFile>.Success(mediaFile);
    }

    public Result<MediaFile> Delete()
    {
        RaiseDomainEvent(new FileDeleted(Id, OwnerId, OwnerType));
        return Result<MediaFile>.Success(this);
    }

    [ExcludeFromCodeCoverage]
    protected override void When(DomainEvent domainEvent)
    {
    }
}
