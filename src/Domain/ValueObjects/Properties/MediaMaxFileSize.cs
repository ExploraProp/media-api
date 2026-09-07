using Media.Domain.ValueObjects.Errors;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record MediaMaxFileSize
{
    public long Value { get; }

    [ExcludeFromCodeCoverage]
    private MediaMaxFileSize() => Value = 0;

    private MediaMaxFileSize(long value) => Value = value;

    public static Result<MediaMaxFileSize> Create(long value)
    {
        if (value <= 0)
            return Result<MediaMaxFileSize>.Failure(MediaMaxFileSizeError.NonPositive);

        return Result<MediaMaxFileSize>.Success(new MediaMaxFileSize(value));
    }

    public static MediaMaxFileSize FromDatabase(long value) => new(value);
}
