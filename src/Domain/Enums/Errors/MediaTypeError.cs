using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Errors;

public static class MediaTypeError
{
    public static readonly Error Invalid = ValueObjectFailure.InvalidFormat(nameof(MediaType));
}
