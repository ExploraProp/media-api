using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Application.Dto;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.Dto;

public sealed record MediaVariantMetadataDto(int? Width, int? Height);

public sealed record MediaVariantDto(
    CatalogLabelDto Type,
    string Url,
    MediaVariantMetadataDto? Metadata,
    long FileSizeBytes)
{
    public static MediaVariantDto FromEntity(MediaVariant variant, ICatalogLabelLocalizer labels)
    {
        MediaVariantMetadataDto? metadata = variant.Metadata is null
            ? null
            : new(variant.Metadata.Width, variant.Metadata.Height);

        return new(
            CatalogLabelDto.From(variant.VariantType.ToI18nCode(), labels),
            variant.Url.Value,
            metadata,
            variant.FileSizeBytes.Value);
    }
}
