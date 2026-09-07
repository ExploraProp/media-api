using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.Dto;

public sealed record MediaVariantDto(
    string Type,
    string TypeLabel,
    string Url,
    int? Width,
    int? Height,
    long FileSizeBytes)
{
    public static MediaVariantDto FromEntity(MediaVariant variant, ICatalogLabelLocalizer labels)
    {
        var type = variant.VariantType.ToI18nCode();
        return new(
            type,
            labels.Localize(type),
            variant.Url.Value,
            variant.Width?.Value,
            variant.Height?.Value,
            variant.FileSizeBytes.Value);
    }
}
