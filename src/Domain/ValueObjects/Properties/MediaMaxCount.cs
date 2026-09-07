using Media.Domain.ValueObjects.Errors;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record MediaMaxCount
{
    public int Value { get; }

    [ExcludeFromCodeCoverage]
    private MediaMaxCount() => Value = 0;

    private MediaMaxCount(int value) => Value = value;

    public static Result<MediaMaxCount> Create(int value)
    {
        if (value <= 0)
            return Result<MediaMaxCount>.Failure(MediaMaxCountError.Invalid);

        return Result<MediaMaxCount>.Success(new MediaMaxCount(value));
    }

    public static MediaMaxCount FromDatabase(int value) => new(value);
}
