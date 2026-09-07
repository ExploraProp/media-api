using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.ValueObjects.Errors;

public static class MediaFileSizeError
{
    public static readonly Error InvalidFormat = ValueObjectFailure.InvalidFormat(nameof(MediaFileSize));
}
