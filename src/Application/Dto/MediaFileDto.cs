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
    CatalogLabelDto Status,
    string? PublicUrl,
    CatalogLabelDto OwnerType,
    string UploadedAt
) : BaseDto(Id, CreatedAt, UpdatedAt)
{
    public IReadOnlyList<MediaVariantDto> Variants { get; init; } = [];

    public static MediaFileDto FromEntity(
        MediaFile entity,
        AccessLevel accessLevel,
        ICatalogLabelLocalizer labels)
    {
        // Ready row CreatedAt is approval time; keep UploadedAt in the API shape as that instant.
        var readyAt = entity.CreatedAt.ToIso();

        return new(
            entity.Id.ToString(),
            readyAt,
            entity.UpdatedAt.ToIso(),
            CatalogLabelDto.From(MediaStatus.Approved.ToI18nCode(), labels),
            entity.ResolvePublicUrl(accessLevel),
            CatalogLabelDto.From(entity.OwnerType.ToI18nCode(), labels),
            readyAt)
        {
            Variants = entity.Variants.Select(v => MediaVariantDto.FromEntity(v, labels)).ToList().AsReadOnly()
        };
    }

    public static MediaFileDto FromEntity(MediaUpload entity, ICatalogLabelLocalizer labels) =>
        new(
            entity.Id.ToString(),
            entity.CreatedAt.ToIso(),
            entity.UpdatedAt.ToIso(),
            CatalogLabelDto.From(entity.Status.ToI18nCode(), labels),
            PublicUrl: null,
            CatalogLabelDto.From(entity.OwnerType.ToI18nCode(), labels),
            entity.UploadedAt.ToIso())
        {
            Variants = []
        };
}
