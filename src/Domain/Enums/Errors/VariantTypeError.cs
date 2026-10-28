using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Errors;

public static class VariantTypeError
{
    public static readonly Error Invalid = ValueObjectFailure.InvalidFormat(nameof(VariantType));
    public static readonly Error Empty = ValueObjectFailure.Empty(nameof(VariantType));
}
