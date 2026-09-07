namespace Media.Domain.Enums.Properties;

[Flags]
public enum AllowedContentTypes
{
    None = 0,
    Jpeg = 1,
    Png = 2,
    Gif = 4,
    WebP = 8,
    Pdf = 16,
}

public static class AllowedContentTypesExtensions
{
    public const int RequiredMagicByteHeaderLength = 12;

    public static AllowedContentTypes? FromMagicBytes(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return AllowedContentTypes.Jpeg;

        if (header.Length >= 4 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return AllowedContentTypes.Png;

        if (header.Length >= 4 &&
            header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
            return AllowedContentTypes.Gif;

        if (header.Length >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            return AllowedContentTypes.WebP;

        if (header.Length >= 4 &&
            header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
            return AllowedContentTypes.Pdf;

        return null;
    }

    public static bool IsImage(this AllowedContentTypes contentType) =>
        contentType is AllowedContentTypes.Jpeg
            or AllowedContentTypes.Png
            or AllowedContentTypes.Gif
            or AllowedContentTypes.WebP;

    public static bool IsDocument(this AllowedContentTypes contentType) =>
        contentType == AllowedContentTypes.Pdf;

    public static bool IsAllowed(this AllowedContentTypes detected, AllowedContentTypes policyFlags) =>
        detected != AllowedContentTypes.None && policyFlags.HasFlag(detected);

    public static bool IsNotAllowed(this AllowedContentTypes detected, AllowedContentTypes policyFlags) =>
        !IsAllowed(detected, policyFlags);

    public static string ToMimeType(this AllowedContentTypes contentType) => contentType switch
    {
        AllowedContentTypes.Jpeg => "image/jpeg",
        AllowedContentTypes.Png => "image/png",
        AllowedContentTypes.Gif => "image/gif",
        AllowedContentTypes.WebP => "image/webp",
        AllowedContentTypes.Pdf => "application/pdf",
        _ => "application/octet-stream",
    };

    public static string ToFileExtension(this AllowedContentTypes contentType) => contentType switch
    {
        AllowedContentTypes.Jpeg => ".jpg",
        AllowedContentTypes.Png => ".png",
        AllowedContentTypes.Gif => ".gif",
        AllowedContentTypes.WebP => ".webp",
        AllowedContentTypes.Pdf => ".pdf",
        _ => string.Empty,
    };
}
