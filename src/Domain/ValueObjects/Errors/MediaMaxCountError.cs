using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.ValueObjects.Errors;

public static class MediaMaxCountError
{
    public static readonly Error Invalid = ValueObjectFailure.InvalidFormat(nameof(MediaMaxCount));
}
