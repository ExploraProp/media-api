using Media.Domain.ValueObjects.Errors;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record MediaFileSize
{
    public long Value { get; }

    [ExcludeFromCodeCoverage]
    private MediaFileSize() => Value = 0;

    private MediaFileSize(long value) => Value = value;

    public static Result<MediaFileSize> Create(long value)
    {
        if (value <= 0)
            return Result<MediaFileSize>.Failure(MediaFileSizeError.InvalidFormat);

        return Result<MediaFileSize>.Success(new MediaFileSize(value));
    }

    public static MediaFileSize FromDatabase(long value) => new(value);
}
