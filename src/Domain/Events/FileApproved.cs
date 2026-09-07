using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Domain;

namespace Media.Domain.Events;

public sealed record MediaVariantSnapshot(string Type, string Url);

public abstract record MediaFileEvents : DomainEvent
{
    public override string AggregateType => nameof(Entities.Definitions.MediaFile);
    public MediaFileId MediaFileId { get; init; } = null!;

    protected MediaFileEvents(MediaFileId mediaFileId)
    {
        MediaFileId = mediaFileId;
        AggregateId = mediaFileId.Value;
    }
}

public sealed record FileApproved(
    string? PublicUrl,
    OwnerId OwnerId,
    OwnerType OwnerType,
    AccessLevel AccessLevel,
    IReadOnlyList<MediaVariantSnapshot> Variants,
    MediaFileId MediaFileId) : MediaFileEvents(MediaFileId);

public sealed record FileRejected(
    OwnerId OwnerId,
    OwnerType OwnerType,
    string Reason,
    MediaFileId MediaFileId) : MediaFileEvents(MediaFileId);

public sealed record FileDeleted(
    MediaFileId MediaFileId,
    OwnerId OwnerId,
    OwnerType OwnerType) : MediaFileEvents(MediaFileId);
