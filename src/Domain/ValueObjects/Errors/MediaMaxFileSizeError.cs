using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.ValueObjects.Errors;

public static class MediaMaxFileSizeError
{
    public static readonly Error NonPositive = ValueObjectFailure.InvalidFormat(nameof(MediaMaxFileSize));
}
