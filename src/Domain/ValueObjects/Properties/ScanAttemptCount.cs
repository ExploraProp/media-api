using Media.Domain.ValueObjects.Errors;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record ScanAttemptCount
{
    public int Value { get; }

    [ExcludeFromCodeCoverage]
    private ScanAttemptCount() => Value = 0;

    private ScanAttemptCount(int value) => Value = value;

    public static Result<ScanAttemptCount> Create(int value)
    {
        if (value < 0)
            return Result<ScanAttemptCount>.Failure(ScanAttemptCountError.Negative);

        return Result<ScanAttemptCount>.Success(new ScanAttemptCount(value));
    }

    public static ScanAttemptCount FromDatabase(int value) => new(value);
}
