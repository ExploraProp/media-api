using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Domain;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.Entities.Definitions;

public sealed class MediaVariant : Entity<MediaVariantId>
{
    public MediaFileId MediaFileId { get; private set; } = null!;
    public VariantType VariantType { get; private set; }
    public Link Url { get; private set; } = null!;
    public MediaVariantMetadata? Metadata { get; private set; }
    public MediaFileSize FileSizeBytes { get; private set; } = null!;

    private MediaVariant(MediaVariantId id) : base(id) { }

    [ExcludeFromCodeCoverage]
    private MediaVariant() : base(new MediaVariantId(Guid.Empty)) { }

    public static MediaVariant Create(
        MediaFileId mediaFileId,
        VariantType variantType,
        Link url,
        MediaVariantMetadata? metadata,
        MediaFileSize fileSizeBytes)
        => new(MediaVariantId.New())
        {
            MediaFileId = mediaFileId,
            VariantType = variantType,
            Url = url,
            Metadata = metadata,
            FileSizeBytes = fileSizeBytes,
        };
}
