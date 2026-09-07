using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Errors;

public static class AccessLevelError
{
    public static readonly Error Invalid = ValueObjectFailure.InvalidFormat(nameof(AccessLevel));
}
