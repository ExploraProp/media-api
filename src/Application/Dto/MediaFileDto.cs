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
    string UploadedAt
) : BaseDto(Id, CreatedAt, UpdatedAt)
{
    public IReadOnlyList<MediaVariantDto> Variants { get; init; } = [];

    public static MediaFileDto FromEntity(
        MediaFile entity,
        AccessLevel accessLevel,
        ICatalogLabelLocalizer labels)
    {
        var status = MediaStatus.Approved.ToI18nCode();
        var ownerType = entity.OwnerType.ToI18nCode();
        // Ready row CreatedAt is approval time; keep UploadedAt in the API shape as that instant.
        var readyAt = entity.CreatedAt.ToIso();

        return new(
            entity.Id.ToString(),
            readyAt,
            entity.UpdatedAt.ToIso(),
            status,
            labels.Localize(status),
            entity.ResolvePublicUrl(accessLevel),
            ownerType,
            labels.Localize(ownerType),
            readyAt)
        {
            Variants = entity.Variants.Select(v => MediaVariantDto.FromEntity(v, labels)).ToList().AsReadOnly()
        };
    }

    public static MediaFileDto FromEntity(MediaUpload entity, ICatalogLabelLocalizer labels)
    {
        var status = entity.Status.ToI18nCode();
        var ownerType = entity.OwnerType.ToI18nCode();

        return new(
            entity.Id.ToString(),
            entity.CreatedAt.ToIso(),
            entity.UpdatedAt.ToIso(),
            status,
            labels.Localize(status),
            PublicUrl: null,
            ownerType,
            labels.Localize(ownerType),
            entity.UploadedAt.ToIso())
        {
            Variants = []
        };
    }
}
