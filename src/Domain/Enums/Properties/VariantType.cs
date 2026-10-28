using Media.Domain.Enums.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Properties;

public enum VariantType
{
    Original = 0,
    Thumbnail = 1,
    Medium = 2,
    Large = 3,
}

public static class VariantTypeExtensions
{
    public static (int Width, int Height) GetDimensions(this VariantType variantType) => variantType switch
    {
        VariantType.Thumbnail => (200, 200),
        VariantType.Medium => (800, 600),
        VariantType.Large => (1920, 1080),
        _ => throw new ArgumentOutOfRangeException(nameof(variantType), variantType, null),
    };

    public static string FileName(this VariantType variantType, AllowedContentTypes contentType) =>
        variantType == VariantType.Original
            ? $"{variantType.ToI18nCode()}{contentType.ToFileExtension()}"
            : $"{variantType.ToI18nCode()}.webp";

    public static bool IsCropped(this VariantType variantType) =>
        variantType == VariantType.Thumbnail;

    public static bool IsWatermarkable(this VariantType variantType) =>
        variantType is VariantType.Medium or VariantType.Large;

    public static string ToI18nCode(this VariantType variantType) => EnumI18n.GetI18nCode(variantType);

    public static Result<VariantType> TryParseVariantType(string? value) =>
        EnumParse.TryParse<VariantType>(value, VariantTypeError.Invalid, VariantTypeError.Empty);

    public static bool TryFromOrdinalVariantType(string? value, out VariantType result) =>
        EnumParse.TryFromOrdinal(value, out result);
}
