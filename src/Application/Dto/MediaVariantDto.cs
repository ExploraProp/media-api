using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.Dto;

public sealed record MediaVariantMetadataDto(int? Width, int? Height);

public sealed record MediaVariantDto(
    string Type,
    string TypeLabel,
    string Url,
    MediaVariantMetadataDto? Metadata,
    long FileSizeBytes)
{
    public static MediaVariantDto FromEntity(MediaVariant variant, ICatalogLabelLocalizer labels)
    {
        var type = variant.VariantType.ToI18nCode();
        MediaVariantMetadataDto? metadata = variant.Metadata is null
            ? null
            : new(variant.Metadata.Width, variant.Metadata.Height);

        return new(
            type,
            labels.Localize(type),
            variant.Url.Value,
            metadata,
            variant.FileSizeBytes.Value);
    }
}
