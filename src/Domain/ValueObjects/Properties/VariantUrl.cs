using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record VariantUrl
{
    public string Value { get; }

    [ExcludeFromCodeCoverage]
    private VariantUrl() => Value = string.Empty;

    private VariantUrl(string value) => Value = value;

    public static VariantUrl FromStorage(string value) => new(value);
}
