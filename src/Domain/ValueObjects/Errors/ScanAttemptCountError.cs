using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.ValueObjects.Errors;

public static class ScanAttemptCountError
{
    public static readonly Error Negative = ValueObjectFailure.InvalidFormat(nameof(ScanAttemptCount));
}
