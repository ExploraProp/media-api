using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record VariantDimension
{
    public int Value { get; }

    [ExcludeFromCodeCoverage]
    private VariantDimension() => Value = 0;

    private VariantDimension(int value) => Value = value;

    public static VariantDimension From(int value) => new(value);
}
