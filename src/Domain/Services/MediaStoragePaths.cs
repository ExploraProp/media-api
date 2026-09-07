using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;

namespace Media.Domain.Services;

public static class MediaStoragePaths
{
    public static string BuildPromotionPath(
        OwnerType ownerType,
        OwnerId ownerId,
        MediaFileId mediaFileId,
        AllowedContentTypes contentType,
        AccessLevel accessLevel)
    {
        var ext = contentType.ToFileExtension();
        var ownerTypeStr = ownerType.ToI18nCode();
        var path = $"{ownerTypeStr}/{ownerId.Value}/{mediaFileId.Value}{ext}";
        return accessLevel == AccessLevel.Public ? path : $"private/{path}";
    }

    public static string BuildBaseStoragePath(OwnerType ownerType, OwnerId ownerId, AccessLevel accessLevel)
    {
        var ownerTypeStr = ownerType.ToI18nCode();
        var path = $"{ownerTypeStr}/{ownerId.Value}";
        return accessLevel == AccessLevel.Public ? path : $"private/{path}";
    }

    public static string BuildVariantPath(string baseStoragePath, MediaFileId mediaFileId, string fileName) =>
        $"{baseStoragePath}/{mediaFileId.Value}/{fileName}";
}
