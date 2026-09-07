using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Application.Dto;
using SharedKernel.Application.Extensions;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.Dto;

public sealed record MediaFileDto(
    string Id,
    string CreatedAt,
    string UpdatedAt,
    string Status,
    string StatusLabel,
    string? PublicUrl,
    string OwnerType,
    string OwnerTypeLabel,
    string MediaType,
    string MediaTypeLabel,
    string UploadedAt
) : BaseDto(Id, CreatedAt, UpdatedAt)
{
    public IReadOnlyList<MediaVariantDto> Variants { get; init; } = [];

    public static MediaFileDto FromEntity(MediaFile entity, ICatalogLabelLocalizer labels)
    {
        var status = entity.Status.ToI18nCode();
        var ownerType = entity.OwnerType.ToI18nCode();
        var mediaType = entity.MediaType.ToI18nCode();

        return new(
            entity.Id.ToString(),
            entity.CreatedAt.ToIso(),
            entity.UpdatedAt.ToIso(),
            status,
            labels.Localize(status),
            entity.PublicUrl?.Value,
            ownerType,
            labels.Localize(ownerType),
            mediaType,
            labels.Localize(mediaType),
            entity.UploadedAt.ToIso())
        {
            Variants = entity.Variants.Select(v => MediaVariantDto.FromEntity(v, labels)).ToList().AsReadOnly()
        };
    }
}
