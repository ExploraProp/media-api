using Media.Domain.Enums.Properties;

namespace Media.Domain.ValueObjects.Properties;

/// <summary>
/// Storage-ready variant metadata produced by the promotion pipeline and consumed by the aggregate.
/// </summary>
public sealed record PromotedVariantInput(
    VariantType VariantType,
    VariantUrl Url,
    VariantDimension? Width,
    VariantDimension? Height,
    MediaFileSize FileSizeBytes);
