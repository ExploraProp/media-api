namespace Media.Domain.ValueObjects.Properties;

/// <summary>
/// Type-specific variant attributes (e.g. image dimensions). Null on the entity when N/A (documents).
/// </summary>
public sealed record MediaVariantMetadata(int? Width, int? Height)
{
    public static MediaVariantMetadata ForImage(int width, int height) => new(width, height);

    public static MediaVariantMetadata? FromDimensions(int? width, int? height) =>
        width.HasValue && height.HasValue
            ? ForImage(width.Value, height.Value)
            : null;
}
